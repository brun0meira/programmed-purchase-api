using Domain.Dto.Cliente;
using Domain.Entities;
using Domain.Enums;
using Domain.ExternalServices;
using Domain.Repositories;
using Domain.ExternalServices;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Business
{
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly ICotacaoB3Service _cotacaoB3Service;

        public ClienteService(IClienteRepository clienteRepository, ICotacaoB3Service cotacaoB3Service)
        {
            _clienteRepository = clienteRepository;
            _cotacaoB3Service = cotacaoB3Service;
        }

        public async Task<AdesaoResponseDto> AderirProdutoAsync(AdesaoRequestDto request)
        {
            if (await _clienteRepository.CpfExisteAsync(request.Cpf))
            {
                throw new InvalidOperationException("CLIENTE_CPF_DUPLICADO|CPF ja cadastrado no sistema.");
            }

            var dataAtual = DateTime.UtcNow;

            var novoCliente = new Cliente
            {
                Nome = request.Nome,
                Cpf = request.Cpf,
                Email = request.Email,
                ValorMensal = request.ValorMensal,
                Ativo = true,
                DataAdesao = dataAtual
            };

            // Criar Conta Gráfica Filhote automaticamente na adesão
            var contaGrafica = new ContaGrafica
            {
                NumeroConta = $"FLH-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}", // Simulando um gerador de número de conta
                Tipo = TipoConta.Filhote,
                DataCriacao = dataAtual
            };

            novoCliente.ContasGraficas.Add(contaGrafica);

            await _clienteRepository.AdicionarAsync(novoCliente);
            await _clienteRepository.SalvarAlteracoesAsync();

            return new AdesaoResponseDto
            {
                ClienteId = novoCliente.Id,
                Nome = novoCliente.Nome,
                Cpf = novoCliente.Cpf,
                Email = novoCliente.Email,
                ValorMensal = novoCliente.ValorMensal,
                Ativo = novoCliente.Ativo,
                DataAdesao = novoCliente.DataAdesao,
                ContaGrafica = new ContaGraficaDto
                {
                    Id = contaGrafica.Id,
                    NumeroConta = contaGrafica.NumeroConta,
                    Tipo = contaGrafica.Tipo.ToString().ToUpper(),
                    DataCriacao = contaGrafica.DataCriacao
                }
            };
        }

        public async Task<SaidaResponseDto> SairProdutoAsync(long clienteId)
        {
            var cliente = await _clienteRepository.ObterPorIdAsync(clienteId);

            if (cliente == null)
                throw new InvalidOperationException("CLIENTE_NAO_ENCONTRADO|Cliente nao encontrado.");

            if (!cliente.Ativo)
                throw new InvalidOperationException("CLIENTE_JA_INATIVO|Cliente ja havia saido do produto.");

            cliente.Ativo = false;

            await _clienteRepository.AtualizarAsync(cliente);
            await _clienteRepository.SalvarAlteracoesAsync();

            return new SaidaResponseDto
            {
                ClienteId = cliente.Id,
                Nome = cliente.Nome,
                Ativo = cliente.Ativo,
                DataSaida = DateTime.UtcNow,
                Mensagem = "Adesao encerrada. Sua posicao em custodia foi mantida."
            };
        }

        public async Task<AlterarValorMensalResponseDto> AlterarValorMensalAsync(long clienteId, AlterarValorMensalRequestDto request)
        {
            var cliente = await _clienteRepository.ObterPorIdAsync(clienteId);

            if (cliente == null)
                throw new InvalidOperationException("CLIENTE_NAO_ENCONTRADO|Cliente nao encontrado.");

            var valorAnterior = cliente.ValorMensal;
            cliente.ValorMensal = request.NovoValorMensal;

            await _clienteRepository.AtualizarAsync(cliente);
            await _clienteRepository.SalvarAlteracoesAsync();

            return new AlterarValorMensalResponseDto
            {
                ClienteId = cliente.Id,
                ValorMensalAnterior = valorAnterior,
                ValorMensalNovo = cliente.ValorMensal,
                DataAlteracao = DateTime.UtcNow,
                Mensagem = "Valor mensal atualizado. O novo valor sera considerado a partir da proxima data de compra."
            };
        }

        public async Task<ConsultaCarteiraResponseDto> ConsultarCarteiraAsync(long clienteId)
        {
            var cliente = await _clienteRepository.ObterClienteComCustodiaAsync(clienteId);

            if (cliente == null)
                throw new InvalidOperationException("CLIENTE_NAO_ENCONTRADO|Cliente nao encontrado.");

            var contaGrafica = cliente.ContasGraficas.FirstOrDefault(c => c.Tipo == TipoConta.Filhote);
            var custodias = contaGrafica?.Custodias ?? new List<Custodia>();

            // Pegamos apenas os tickers únicos que o cliente tem
            var tickersDaCarteira = custodias.Select(c => c.Ticker).Distinct().ToList();

            var cotacoesAtuais = new Dictionary<string, decimal>();
            if (tickersDaCarteira.Any())
            {
                // Simula que "hoje" é o dia do último arquivo COTAHIST (Ex: 25/02/2026).
                // Em produção, usaríamos DateTime.UtcNow ou leríamos a última cotação salva em cache/banco.
                var dataSimulacaoHoje = new DateTime(2026, 2, 25);
                cotacoesAtuais = await _cotacaoB3Service.ObterCotacoesFechamentoAsync(dataSimulacaoHoje, tickersDaCarteira);
            }

            var response = new ConsultaCarteiraResponseDto
            {
                ClienteId = cliente.Id,
                Nome = cliente.Nome,
                ContaGrafica = contaGrafica?.NumeroConta ?? string.Empty,
                DataConsulta = DateTime.UtcNow,
                Resumo = new ResumoCarteiraDto(),
                Ativos = new List<AtivoCarteiraDto>()
            };

            decimal valorTotalInvestido = 0;
            decimal valorAtualCarteira = 0;

            foreach (var custodia in custodias)
            {
                var cotacaoAtual = cotacoesAtuais.ContainsKey(custodia.Ticker) ? cotacoesAtuais[custodia.Ticker] : custodia.PrecoMedio;

                var valorInvestidoAtivo = custodia.Quantidade * custodia.PrecoMedio;
                var valorAtualAtivo = custodia.Quantidade * cotacaoAtual;
                var plAtivo = valorAtualAtivo - valorInvestidoAtivo;

                var ativoDto = new AtivoCarteiraDto
                {
                    Ticker = custodia.Ticker,
                    Quantidade = custodia.Quantidade,
                    PrecoMedio = custodia.PrecoMedio,
                    CotacaoAtual = cotacaoAtual,
                    ValorAtual = valorAtualAtivo,
                    Pl = plAtivo,
                    PlPercentual = valorInvestidoAtivo > 0 ? Math.Round((plAtivo / valorInvestidoAtivo) * 100, 2) : 0
                };

                response.Ativos.Add(ativoDto);

                valorTotalInvestido += valorInvestidoAtivo;
                valorAtualCarteira += valorAtualAtivo;
            }

            response.Resumo.ValorTotalInvestido = valorTotalInvestido;
            response.Resumo.ValorAtualCarteira = valorAtualCarteira;
            response.Resumo.PlTotal = valorAtualCarteira - valorTotalInvestido;

            response.Resumo.RentabilidadePercentual = valorTotalInvestido > 0
                ? Math.Round((response.Resumo.PlTotal / valorTotalInvestido) * 100, 2)
                : 0;

            foreach (var ativo in response.Ativos)
            {
                ativo.ComposicaoCarteira = valorAtualCarteira > 0
                    ? Math.Round((ativo.ValorAtual / valorAtualCarteira) * 100, 2)
                    : 0;
            }

            return response;
        }

        public async Task<RentabilidadeDetalhadaResponseDto> ConsultarRentabilidadeDetalhadaAsync(long clienteId)
        {
            var cliente = await _clienteRepository.ObterClienteComHistoricoAsync(clienteId);

            if (cliente == null)
                throw new InvalidOperationException("CLIENTE_NAO_ENCONTRADO|Cliente nao encontrado.");

            var contaGrafica = cliente.ContasGraficas.FirstOrDefault(c => c.Tipo == TipoConta.Filhote);
            var custodias = contaGrafica?.Custodias ?? new List<Custodia>();

            var tickersDaCarteira = custodias.Select(c => c.Ticker).Distinct().ToList();

            var cotacoesAtuais = new Dictionary<string, decimal>();
            if (tickersDaCarteira.Any())
            {
                // Simula que "hoje" é o dia do último arquivo COTAHIST (Ex: 25/02/2026).
                // Em produção, usaríamos DateTime.UtcNow ou leríamos a última cotação salva em cache/banco.
                var dataSimulacaoHoje = new DateTime(2026, 2, 25);
                cotacoesAtuais = await _cotacaoB3Service.ObterCotacoesFechamentoAsync(dataSimulacaoHoje, tickersDaCarteira);
            }

            var response = new RentabilidadeDetalhadaResponseDto
            {
                ClienteId = cliente.Id,
                Nome = cliente.Nome,
                DataConsulta = DateTime.UtcNow,
                Rentabilidade = new ResumoCarteiraDto()
            };

            decimal valorTotalInvestidoAtual = 0;
            decimal valorAtualCarteira = 0;

            foreach (var custodia in custodias)
            {
                var cotacaoAtual = cotacoesAtuais.ContainsKey(custodia.Ticker) ? cotacoesAtuais[custodia.Ticker] : custodia.PrecoMedio;
                valorTotalInvestidoAtual += custodia.Quantidade * custodia.PrecoMedio;
                valorAtualCarteira += custodia.Quantidade * cotacaoAtual;
            }

            response.Rentabilidade.ValorTotalInvestido = valorTotalInvestidoAtual;
            response.Rentabilidade.ValorAtualCarteira = valorAtualCarteira;
            response.Rentabilidade.PlTotal = valorAtualCarteira - valorTotalInvestidoAtual;
            response.Rentabilidade.RentabilidadePercentual = valorTotalInvestidoAtual > 0
                ? Math.Round((response.Rentabilidade.PlTotal / valorTotalInvestidoAtual) * 100, 2) : 0;

            var todasDistribuicoes = custodias.SelectMany(c => c.Distribuicoes)
                                              .OrderBy(d => d.DataDistribuicao)
                                              .ToList();

            var distribuicoesPorData = todasDistribuicoes.GroupBy(d => d.DataDistribuicao.Date).ToList();

            decimal valorInvestidoAcumulado = 0;
            int contadorParcela = 1;

            foreach (var grupo in distribuicoesPorData)
            {
                var valorAporteDia = grupo.Sum(d => d.Quantidade * d.PrecoUnitario);
                valorInvestidoAcumulado += valorAporteDia;

                response.HistoricoAportes.Add(new HistoricoAporteDto
                {
                    Data = grupo.Key.ToString("yyyy-MM-dd"),
                    Valor = Math.Round(valorAporteDia, 2),
                    Parcela = $"{contadorParcela}/3"
                });

                response.EvolucaoCarteira.Add(new EvolucaoCarteiraDto
                {
                    Data = grupo.Key.ToString("yyyy-MM-dd"),
                    ValorInvestido = Math.Round(valorInvestidoAcumulado, 2),
                    ValorCarteira = Math.Round(valorInvestidoAcumulado * 1.02m, 2), // Mock de evolução
                    Rentabilidade = 2.00m
                });

                contadorParcela = contadorParcela >= 3 ? 1 : contadorParcela + 1;
            }

            return response;
        }
    }
}