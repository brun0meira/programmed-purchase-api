using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Dto.Admin;
using Domain.Entities;
using Domain.Enums;
using Domain.ExternalServices;
using Domain.Repositories;

namespace Domain.Business
{
    public class ContaMasterService : IContaMasterService
    {
        private readonly IContaRepository _contaRepository;
        private readonly ICotacaoB3Service _cotacaoB3Service;

        public ContaMasterService(IContaRepository contaRepository, ICotacaoB3Service cotacaoB3Service)
        {
            _contaRepository = contaRepository;
            _cotacaoB3Service = cotacaoB3Service;
        }

        public async Task<CustodiaMasterResponseDto> ConsultarCustodiaMasterAsync()
        {
            var contaMaster = await _contaRepository.ObterContaMasterComCustodiaAsync();

            // Se a conta master ainda não existir no banco, criamos ela.
            if (contaMaster == null)
            {
                contaMaster = new ContaGrafica
                {
                    NumeroConta = "MST-000001",
                    Tipo = TipoConta.Master,
                    DataCriacao = DateTime.UtcNow
                };
                await _contaRepository.AdicionarAsync(contaMaster);
                await _contaRepository.SalvarAlteracoesAsync();
            }

            var tickersNaMaster = contaMaster.Custodias.Select(c => c.Ticker).Distinct().ToList();

            var cotacoesAtuais = new Dictionary<string, decimal>();
            if (tickersNaMaster.Any())
            {
                // Simula que "hoje" é o dia do último arquivo COTAHIST (Ex: 25/02/2026).
                // Em produção, usaríamos DateTime.UtcNow ou leríamos a última cotação salva em cache/banco.
                var dataSimulacaoHoje = new DateTime(2026, 2, 25);
                cotacoesAtuais = await _cotacaoB3Service.ObterCotacoesFechamentoAsync(dataSimulacaoHoje, tickersNaMaster);
            }

            var response = new CustodiaMasterResponseDto
            {
                ContaMaster = new ContaMasterDto
                {
                    Id = contaMaster.Id,
                    NumeroConta = contaMaster.NumeroConta,
                    Tipo = contaMaster.Tipo.ToString().ToUpper()
                }
            };

            decimal valorTotalResiduo = 0;

            foreach (var custodia in contaMaster.Custodias)
            {
                var cotacaoAtual = cotacoesAtuais.ContainsKey(custodia.Ticker) ? cotacoesAtuais[custodia.Ticker] : custodia.PrecoMedio;
                var valorAtual = custodia.Quantidade * cotacaoAtual;

                var dataFormatada = custodia.DataUltimaAtualizacao.ToString("yyyy-MM-dd");

                response.Custodia.Add(new ItemCustodiaMasterDto
                {
                    Ticker = custodia.Ticker,
                    Quantidade = custodia.Quantidade,
                    PrecoMedio = custodia.PrecoMedio,
                    ValorAtual = valorAtual,
                    Origem = $"Residuo distribuicao {dataFormatada}"
                });

                valorTotalResiduo += valorAtual;
            }

            response.ValorTotalResiduo = valorTotalResiduo;

            return response;
        }
    }
}