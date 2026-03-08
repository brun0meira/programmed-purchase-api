using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Domain.ExternalServices;
using Domain.Repositories;

namespace Domain.Business
{
    public class RebalanceamentoService : IRebalanceamentoService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly ICestaRepository _cestaRepository;
        private readonly ICotacaoB3Service _cotacaoB3Service;
        private readonly IKafkaProducerService _kafkaProducerService;
        private readonly IContaRepository _contaRepository;
        private readonly IRebalanceamentoRepository _rebalanceamentoRepository; // <-- NOVO AQUI

        public RebalanceamentoService(
            IClienteRepository clienteRepository,
            ICestaRepository cestaRepository,
            ICotacaoB3Service cotacaoB3Service,
            IKafkaProducerService kafkaProducerService,
            IContaRepository contaRepository,
            IRebalanceamentoRepository rebalanceamentoRepository) // <-- NOVO AQUI
        {
            _clienteRepository = clienteRepository;
            _cestaRepository = cestaRepository;
            _cotacaoB3Service = cotacaoB3Service;
            _kafkaProducerService = kafkaProducerService;
            _contaRepository = contaRepository;
            _rebalanceamentoRepository = rebalanceamentoRepository;
        }

        public async Task<string> ExecutarRebalanceamentoAsync(DateTime dataReferencia)
        {
            var cestaAtual = await _cestaRepository.ObterCestaAtualAsync();
            if (cestaAtual == null)
                throw new InvalidOperationException("REBALANCEAMENTO_FALHOU|Nenhuma cesta ativa encontrada.");

            var clientes = await _clienteRepository.ObterClientesAtivosComCustodiaAsync();
            if (!clientes.Any())
                return "Nenhum cliente ativo para rebalancear.";

            var tickersCestaAtual = cestaAtual.Itens.Select(i => i.Ticker).ToList();

            // Coletar TODOS os tickers (da cesta nova + o que os clientes já têm na carteira) para buscar a cotação
            var tickersNaCustodia = clientes
                .SelectMany(c => c.ContasGraficas.SelectMany(cg => cg.Custodias.Select(cus => cus.Ticker)))
                .ToList();

            var todosTickersUnicos = tickersCestaAtual.Union(tickersNaCustodia).Distinct().ToList();
            // Busca os preços de fechamento no arquivo COTAHIST da B3
            var cotacoes = await _cotacaoB3Service.ObterCotacoesFechamentoAsync(dataReferencia, todosTickersUnicos);

            int clientesRebalanceados = 0;

            foreach (var cliente in clientes)
            {
                var contaFilhote = cliente.ContasGraficas.FirstOrDefault(c => c.Tipo == TipoConta.Filhote);
                if (contaFilhote == null) continue;

                decimal caixaGeradoComVendas = 0m;

                // Busca o histórico de vendas do mês (necessário para a isenção dos 20 mil)
                decimal totalVendasMes = await _clienteRepository.ObterTotalVendasMesAsync(cliente.Id, dataReferencia.Month, dataReferencia.Year);

                // Etapa A: Identificar e vender ativos que sairam da cesta (ou seja, que o cliente tem mas a nova cesta não tem)
                var custodiasParaVender = contaFilhote.Custodias
                    .Where(c => !tickersCestaAtual.Contains(c.Ticker) && c.Quantidade > 0)
                    .ToList();

                foreach (var custodia in custodiasParaVender)
                {
                    if (!cotacoes.TryGetValue(custodia.Ticker, out var precoVenda)) continue;

                    decimal valorVenda = custodia.Quantidade * precoVenda;
                    decimal custoAquisicao = custodia.Quantidade * custodia.PrecoMedio;
                    decimal lucroLiquido = valorVenda - custoAquisicao;

                    caixaGeradoComVendas += valorVenda;
                    totalVendasMes += valorVenda;

     
                    await _rebalanceamentoRepository.AdicionarAsync(new Rebalanceamento
                    {
                        ClienteId = cliente.Id,
                        Tipo = (TipoRebalanceamento)1,
                        TickerVendido = custodia.Ticker,
                        TickerComprado = "NENHUM",
                        ValorVenda = valorVenda,
                        DataRebalanceamento = dataReferencia
                    });

                    // Regra da B3: isenção de IR para vendas até 20 mil por mês. Se passar disso, calcula o imposto sobre o lucro e envia para o Kafka
                    if (totalVendasMes > 20000m && lucroLiquido > 0)
                    {
                        decimal impostoDevido = Math.Round(lucroLiquido * 0.20m, 2); // 20% sobre o lucro
                        decimal dedoDuro = Math.Round(valorVenda * 0.00005m, 2); // 0,005% sobre o valor bruto da VENDA

                        // Envia o imposto total
                        await _kafkaProducerService.PublicarEventoIRAsync(
                            cliente.Id,
                            "IR_VENDA_REBALANCEAMENTO",
                            lucroLiquido,
                            impostoDevido + dedoDuro, // Somando os dois
                            dataReferencia.ToString("yyyy-MM-dd")
                        );
                    }

                    // Zera a custódia do ativo (ele foi 100% vendido)
                    custodia.Quantidade = 0;
                }

                // Etapa B: Recomprar os ativos da nova cesta seguindo os percentuais, usando o caixa gerado com as vendas
                if (caixaGeradoComVendas > 0)
                {
                    foreach (var itemCesta in cestaAtual.Itens)
                    {
                        if (!cotacoes.TryGetValue(itemCesta.Ticker, out var cotacaoFechamento)) continue;

                        decimal valorParaComprar = caixaGeradoComVendas * (itemCesta.Percentual / 100m);
                        int qtdComprar = (int)Math.Floor(valorParaComprar / cotacaoFechamento);

                        if (qtdComprar > 0)
                        {
                            var custodiaAlvo = contaFilhote.Custodias.FirstOrDefault(c => c.Ticker == itemCesta.Ticker);
                            if (custodiaAlvo == null)
                            {
                                custodiaAlvo = new Custodia { Ticker = itemCesta.Ticker, Quantidade = 0, PrecoMedio = 0 };
                                contaFilhote.Custodias.Add(custodiaAlvo);
                            }

                            // Matemática do Preço Médio
                            decimal valorAntigo = custodiaAlvo.Quantidade * custodiaAlvo.PrecoMedio;
                            decimal valorNovo = qtdComprar * cotacaoFechamento;
                            custodiaAlvo.Quantidade += qtdComprar;
                            custodiaAlvo.PrecoMedio = Math.Round((valorAntigo + valorNovo) / custodiaAlvo.Quantidade, 4);

                            // Registra a compra no histórico de rebalanceamento
                            await _rebalanceamentoRepository.AdicionarAsync(new Rebalanceamento
                            {
                                ClienteId = cliente.Id,
                                Tipo = (TipoRebalanceamento)1,
                                TickerVendido = "NENHUM",
                                TickerComprado = itemCesta.Ticker,
                                ValorVenda = 0,
                                DataRebalanceamento = dataReferencia
                            });
                        }
                    }
                }
                clientesRebalanceados++;
            }

            // O commit final no banco dispara os Inserts do repositório novo e os Updates da Custódia
            await _contaRepository.SalvarAlteracoesAsync();

            return $"Rebalanceamento concluído com sucesso para {clientesRebalanceados} clientes.";
        }
    }
}