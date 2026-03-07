using Domain.Dto.Admin;
using Domain.Entities;
using Domain.ExternalServices;
using Domain.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Business
{
    public class CestaService : ICestaService
    {
        private readonly ICestaRepository _cestaRepository;
        private readonly ICotacaoB3Service _cotacaoB3Service;

        public CestaService(ICestaRepository cestaRepository, ICotacaoB3Service cotacaoB3Service)
        {
            _cestaRepository = cestaRepository;
            _cotacaoB3Service = cotacaoB3Service;
        }

        public async Task<CestaResponseDto> CadastrarCestaAsync(CriarCestaRequestDto request)
        {
            // Exatamente 5 ativos
            if (request.Itens == null || request.Itens.Count != 5)
            {
                var qtd = request.Itens?.Count ?? 0;
                throw new InvalidOperationException($"QUANTIDADE_ATIVOS_INVALIDA|A cesta deve conter exatamente 5 ativos. Quantidade informada: {qtd}.");
            }

            // Soma exata de 100%
            var soma = request.Itens.Sum(i => i.Percentual);
            if (soma != 100.00m)
            {
                throw new InvalidOperationException($"PERCENTUAIS_INVALIDOS|A soma dos percentuais deve ser exatamente 100%. Soma atual: {soma}%.");
            }

            var dataAtual = DateTime.UtcNow;
            var cestaAnterior = await _cestaRepository.ObterCestaAtualAsync();
            var response = new CestaResponseDto();

            // Desativa a cesta atual se existir e calcula diferenças
            if (cestaAnterior != null)
            {
                cestaAnterior.Ativa = false;
                cestaAnterior.DataDesativacao = dataAtual;
                await _cestaRepository.AtualizarAsync(cestaAnterior);

                var tickersAnteriores = cestaAnterior.Itens.Select(i => i.Ticker).ToList();
                var tickersNovos = request.Itens.Select(i => i.Ticker).ToList();

                response.CestaAnteriorDesativada = new CestaAnteriorDto
                {
                    CestaId = cestaAnterior.Id,
                    Nome = cestaAnterior.Nome,
                    DataDesativacao = dataAtual
                };
                response.RebalanceamentoDisparado = true;
                response.AtivosRemovidos = tickersAnteriores.Except(tickersNovos).ToList();
                response.AtivosAdicionados = tickersNovos.Except(tickersAnteriores).ToList();
                response.Mensagem = "Cesta atualizada. Rebalanceamento disparado para os clientes ativos.";
            }
            else
            {
                response.RebalanceamentoDisparado = false;
                response.Mensagem = "Primeira cesta cadastrada com sucesso.";
            }

            // Cria a nova cesta
            var novaCesta = new CestaRecomendacao
            {
                Nome = request.Nome,
                Ativa = true,
                DataCriacao = dataAtual,
                Itens = request.Itens.Select(i => new ItemCesta
                {
                    Ticker = i.Ticker.ToUpper(),
                    Percentual = i.Percentual
                }).ToList()
            };

            await _cestaRepository.AdicionarAsync(novaCesta);
            await _cestaRepository.SalvarAlteracoesAsync();

            response.CestaId = novaCesta.Id;
            response.Nome = novaCesta.Nome;
            response.Ativa = novaCesta.Ativa;
            response.DataCriacao = novaCesta.DataCriacao;
            response.Itens = novaCesta.Itens.Select(i => new ItemCestaResponseDto
            {
                Ticker = i.Ticker,
                Percentual = i.Percentual
            }).ToList();

            return response;
        }

        public async Task<CestaResponseDto> ConsultarCestaAtualAsync()
        {
            var cesta = await _cestaRepository.ObterCestaAtualAsync();
            if (cesta == null)
                throw new InvalidOperationException("CESTA_NAO_ENCONTRADA|Nenhuma cesta ativa encontrada.");

            var tickers = cesta.Itens.Select(i => i.Ticker).ToList();
            var cotacoesAtuais = new Dictionary<string, decimal>();

            if (tickers.Any())
            {
                // Simula que "hoje" é o dia do último arquivo COTAHIST (Ex: 25/02/2026).
                // Em produção, usaríamos DateTime.UtcNow ou leríamos a última cotação salva em cache/banco.
                var dataSimulacaoHoje = new DateTime(2026, 2, 25);
                cotacoesAtuais = await _cotacaoB3Service.ObterCotacoesFechamentoAsync(dataSimulacaoHoje, tickers);
            }

            return new CestaResponseDto
            {
                CestaId = cesta.Id,
                Nome = cesta.Nome,
                Ativa = cesta.Ativa,
                DataCriacao = cesta.DataCriacao,
                Itens = cesta.Itens.Select(i => new ItemCestaResponseDto
                {
                    Ticker = i.Ticker,
                    Percentual = i.Percentual,
                    CotacaoAtual = cotacoesAtuais.ContainsKey(i.Ticker) ? cotacoesAtuais[i.Ticker] : 0m
                }).ToList()
            };
        }

        public async Task<HistoricoCestasResponseDto> ObterHistoricoCestasAsync()
        {
            var cestas = await _cestaRepository.ObterHistoricoAsync();

            return new HistoricoCestasResponseDto
            {
                Cestas = cestas.Select(c => new CestaResponseDto
                {
                    CestaId = c.Id,
                    Nome = c.Nome,
                    Ativa = c.Ativa,
                    DataCriacao = c.DataCriacao,
                    DataDesativacao = c.DataDesativacao,
                    Itens = c.Itens.Select(i => new ItemCestaResponseDto
                    {
                        Ticker = i.Ticker,
                        Percentual = i.Percentual
                    }).ToList()
                }).ToList()
            };
        }
    }
}