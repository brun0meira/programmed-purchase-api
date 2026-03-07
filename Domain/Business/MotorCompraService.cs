using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Dto.Motor;
using Domain.Entities;
using Domain.Enums;
using Domain.ExternalServices;
using Domain.Repositories;

namespace Domain.Business
{
    public class MotorCompraService : IMotorCompraService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly IContaRepository _contaRepository;
        private readonly ICestaRepository _cestaRepository;
        private readonly ICotacaoB3Service _cotacaoB3Service;
        private readonly IKafkaProducerService _kafkaProducerService;

        public MotorCompraService(
            IClienteRepository clienteRepository,
            IContaRepository contaRepository,
            ICestaRepository cestaRepository,
            ICotacaoB3Service cotacaoB3Service,
            IKafkaProducerService kafkaProducerService)
        {
            _clienteRepository = clienteRepository;
            _contaRepository = contaRepository;
            _cestaRepository = cestaRepository;
            _cotacaoB3Service = cotacaoB3Service;
            _kafkaProducerService = kafkaProducerService;
        }

        public async Task<ExecutarCompraResponseDto> ExecutarCompraProgramadaAsync(ExecutarCompraRequestDto request)
        {
            if (!DateTime.TryParse(request.DataReferencia, out var dataReferencia))
                throw new InvalidOperationException("DATA_INVALIDA|Formato de data inválido. Use yyyy-MM-dd.");

            // 1. Validar pré-requisitos (Clientes, Cesta e Conta Master)
            var clientesAtivos = await _clienteRepository.ObterClientesAtivosComCustodiaAsync();
            if (!clientesAtivos.Any())
                throw new InvalidOperationException("MOTOR_SEM_CLIENTES|Não há clientes ativos para processar.");

            var cestaAtual = await _cestaRepository.ObterCestaAtualAsync();
            if (cestaAtual == null)
                throw new InvalidOperationException("CESTA_NAO_ENCONTRADA|Nenhuma cesta ativa encontrada.");

            var contaMaster = await _contaRepository.ObterContaMasterComCustodiaAsync();
            if (contaMaster == null)
                throw new InvalidOperationException("CONTA_MASTER_NAO_ENCONTRADA|A Conta Master não foi inicializada.");

            // 2. Buscar Cotações do Arquivo B3
            var tickersCesta = cestaAtual.Itens.Select(i => i.Ticker).ToList();
            var cotacoes = await _cotacaoB3Service.ObterCotacoesFechamentoAsync(dataReferencia, tickersCesta);

            // 3. Calcular Valor Total Consolidado (1/3 do aporte de cada cliente)
            var aportesClientes = clientesAtivos.ToDictionary(
                c => c.Id,
                c => Math.Round(c.ValorMensal / 3, 2)
            );
            decimal totalConsolidado = aportesClientes.Values.Sum();

            var response = new ExecutarCompraResponseDto
            {
                DataExecucao = DateTime.UtcNow,
                TotalClientes = clientesAtivos.Count,
                TotalConsolidado = totalConsolidado,
                Mensagem = $"Compra programada executada com sucesso para {clientesAtivos.Count} clientes."
            };

            int eventosKafkaCount = 0;

            // 4. Processar cada Ativo da Cesta Top Five
            foreach (var itemCesta in cestaAtual.Itens)
            {
                if (!cotacoes.TryGetValue(itemCesta.Ticker, out var cotacaoFechamento))
                    continue; // Se não achou a cotação na B3, pula a compra deste ativo

                // Valor total alocado para este ativo segundo o percentual da cesta
                decimal valorAlocadoAtivo = totalConsolidado * (itemCesta.Percentual / 100m);
                int quantidadeNecessariaTotal = (int)Math.Floor(valorAlocadoAtivo / cotacaoFechamento);

                // Descontar o saldo remanescente que já existe na Custódia Master
                var custodiaMaster = contaMaster.Custodias.FirstOrDefault(c => c.Ticker == itemCesta.Ticker);
                int saldoMasterAtual = custodiaMaster?.Quantidade ?? 0;

                int quantidadeComprarMercado = quantidadeNecessariaTotal - saldoMasterAtual;
                if (quantidadeComprarMercado < 0) quantidadeComprarMercado = 0;

                var ordemCompraDto = new OrdemCompraResponseDto
                {
                    Ticker = itemCesta.Ticker,
                    QuantidadeTotal = quantidadeComprarMercado,
                    PrecoUnitario = cotacaoFechamento,
                    ValorTotal = quantidadeComprarMercado * cotacaoFechamento
                };

                OrdemCompra ordemReferencia = null;

                // Executar Compra de Fato (Registrar as ordens dividindo em Lote Padrão e Fracionário)
                if (quantidadeComprarMercado > 0)
                {
                    int lotesPadrao = quantidadeComprarMercado / 100;
                    int restanteFracionario = quantidadeComprarMercado % 100;

                    if (lotesPadrao > 0)
                    {
                        int qtdLote = lotesPadrao * 100;
                        ordemReferencia = new OrdemCompra
                        {
                            Ticker = itemCesta.Ticker,
                            Quantidade = qtdLote,
                            PrecoUnitario = cotacaoFechamento,
                            TipoMercado = TipoMercado.Lote,
                            DataExecucao = dataReferencia
                        };
                        contaMaster.OrdensCompra.Add(ordemReferencia);
                        ordemCompraDto.Detalhes.Add(new OrdemDetalheDto { Tipo = "LOTE_PADRAO", Ticker = itemCesta.Ticker, Quantidade = qtdLote });
                    }

                    if (restanteFracionario > 0)
                    {
                        var ordemFracionaria = new OrdemCompra
                        {
                            Ticker = itemCesta.Ticker,
                            Quantidade = restanteFracionario,
                            PrecoUnitario = cotacaoFechamento,
                            TipoMercado = TipoMercado.Fracionario,
                            DataExecucao = dataReferencia
                        };
                        contaMaster.OrdensCompra.Add(ordemFracionaria);
                        ordemCompraDto.Detalhes.Add(new OrdemDetalheDto { Tipo = "FRACIONARIO", Ticker = $"{itemCesta.Ticker}F", Quantidade = restanteFracionario });

                        if (ordemReferencia == null) ordemReferencia = ordemFracionaria;
                    }
                }
                else
                {
                    // Caso o resíduo da Master cubra 100% da compra, criamos um registro interno para a FK não quebrar
                    ordemReferencia = new OrdemCompra { Ticker = itemCesta.Ticker, Quantidade = 0, PrecoUnitario = cotacaoFechamento, TipoMercado = TipoMercado.Lote, DataExecucao = dataReferencia };
                    contaMaster.OrdensCompra.Add(ordemReferencia);
                }

                response.OrdensCompra.Add(ordemCompraDto);

                // 5. Distribuir para as Contas Filhotes proporcionalmente ao aporte (rateio)
                int acoesDistribuidasTotal = 0;

                foreach (var cliente in clientesAtivos)
                {
                    var aporteCliente = aportesClientes[cliente.Id];
                    decimal proporcaoCliente = aporteCliente / totalConsolidado;

                    int qtdCliente = (int)Math.Floor(quantidadeNecessariaTotal * proporcaoCliente);

                    if (qtdCliente > 0)
                    {
                        var contaFilhote = cliente.ContasGraficas.FirstOrDefault(c => c.Tipo == TipoConta.Filhote);
                        var custodiaCliente = contaFilhote.Custodias.FirstOrDefault(c => c.Ticker == itemCesta.Ticker);

                        // Cria a custódia se o cliente ainda não tiver essa ação
                        if (custodiaCliente == null)
                        {
                            custodiaCliente = new Custodia
                            {
                                Ticker = itemCesta.Ticker,
                                Quantidade = 0,
                                PrecoMedio = 0
                            };
                            contaFilhote.Custodias.Add(custodiaCliente);
                        }

                        // Cálculo do Preço Médio (Regra Financeira: (QtdAntiga*PrecoAntigo + QtdNova*PrecoNovo) / QtdTotal)
                        decimal valorTotalAntigo = custodiaCliente.Quantidade * custodiaCliente.PrecoMedio;
                        decimal valorTotalNovo = qtdCliente * cotacaoFechamento;

                        custodiaCliente.Quantidade += qtdCliente;
                        custodiaCliente.PrecoMedio = Math.Round((valorTotalAntigo + valorTotalNovo) / custodiaCliente.Quantidade, 4);
                        custodiaCliente.DataUltimaAtualizacao = dataReferencia;

                        // Registrar a entidade Distribuição
                        custodiaCliente.Distribuicoes.Add(new Distribuicao
                        {
                            OrdemCompra = ordemReferencia,
                            Ticker = itemCesta.Ticker,
                            Quantidade = qtdCliente,
                            PrecoUnitario = cotacaoFechamento,
                            DataDistribuicao = dataReferencia
                        });

                        acoesDistribuidasTotal += qtdCliente;

                        // Construção do DTO de Resposta da Distribuição
                        var distDto = response.Distribuicoes.FirstOrDefault(d => d.ClienteId == cliente.Id);
                        if (distDto == null)
                        {
                            distDto = new DistribuicaoClienteDto { ClienteId = cliente.Id, Nome = cliente.Nome, ValorAporte = aporteCliente };
                            response.Distribuicoes.Add(distDto);
                        }
                        distDto.Ativos.Add(new AtivoDistribuidoDto { Ticker = itemCesta.Ticker, Quantidade = qtdCliente });

                        // 6. Imposto de Renda (IR Dedo-Duro 0,005%) e Envio para Kafka
                        decimal valorOperacao = qtdCliente * cotacaoFechamento;
                        decimal irDedoDuro = Math.Round(valorOperacao * 0.00005m, 2);

                        if (irDedoDuro > 0)
                        {
                            cliente.EventosIR.Add(new EventoIR
                            {
                                Tipo = TipoEventoIR.DedoDuro,
                                ValorBase = valorOperacao,
                                ValorIR = irDedoDuro,
                                DataEvento = dataReferencia,
                                PublicadoKafka = false
                            });

                            await _kafkaProducerService.PublicarEventoIRAsync(cliente.Id, "DEDO_DURO", valorOperacao, irDedoDuro, request.DataReferencia);
                            eventosKafkaCount++;
                        }
                    }
                }

                // 7. Atualizar a Custódia Master com o que sobrou (Resíduos)
                int acoesRemanescentes = quantidadeNecessariaTotal - acoesDistribuidasTotal;

                if (custodiaMaster == null)
                {
                    custodiaMaster = new Custodia { Ticker = itemCesta.Ticker, Quantidade = 0, PrecoMedio = 0 };
                    contaMaster.Custodias.Add(custodiaMaster);
                }

                // Master assume as sobras ao preço de fechamento
                if (acoesRemanescentes > 0)
                {
                    decimal valorAntigo = custodiaMaster.Quantidade * custodiaMaster.PrecoMedio;
                    decimal valorNovo = acoesRemanescentes * cotacaoFechamento;

                    custodiaMaster.Quantidade = acoesRemanescentes; // A quantidade não acumula, ela é substituída pelo novo resíduo
                    custodiaMaster.PrecoMedio = Math.Round((valorAntigo + valorNovo) / (custodiaMaster.Quantidade == 0 ? 1 : custodiaMaster.Quantidade), 4);
                    custodiaMaster.DataUltimaAtualizacao = dataReferencia;

                    response.ResiduosCustMaster.Add(new ResiduoMasterDto { Ticker = itemCesta.Ticker, Quantidade = acoesRemanescentes });
                }
                else
                {
                    custodiaMaster.Quantidade = 0; // Zerou a Master
                }
            }

            response.EventosIRPublicados = eventosKafkaCount;

            await _contaRepository.SalvarAlteracoesAsync();
            return response;
        }
    }
}