using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Business;
using Domain.Entities;
using Domain.Enums;
using Domain.ExternalServices;
using Domain.Repositories;
using Moq;
using Xunit;

namespace Tests.Services
{
    public class ContaMasterServiceTests
    {
        private readonly Mock<IContaRepository> _contaRepoMock;
        private readonly Mock<ICotacaoB3Service> _cotacaoB3Mock;
        private readonly ContaMasterService _service;

        public ContaMasterServiceTests()
        {
            _contaRepoMock = new Mock<IContaRepository>();
            _cotacaoB3Mock = new Mock<ICotacaoB3Service>();

            _service = new ContaMasterService(_contaRepoMock.Object, _cotacaoB3Mock.Object);
        }

        [Fact(DisplayName = "Consultar Master - Deve criar a conta dinamicamente se não existir")]
        public async Task ConsultarMaster_DeveCriarConta_QuandoNaoExistirNoBanco()
        {
            // Arrange
            _contaRepoMock.Setup(r => r.ObterContaMasterComCustodiaAsync())
                          .ReturnsAsync((ContaGrafica)null);

            // Act
            var resultado = await _service.ConsultarCustodiaMasterAsync();

            // Assert
            Assert.Equal("MST-000001", resultado.ContaMaster.NumeroConta);
            Assert.Empty(resultado.Custodia);
            Assert.Equal(0, resultado.ValorTotalResiduo);

            // Verifica se os métodos de Adicionar e Salvar foram chamados para gravar a nova conta
            _contaRepoMock.Verify(r => r.AdicionarAsync(It.Is<ContaGrafica>(c => c.Tipo == TipoConta.Master)), Times.Once);
            _contaRepoMock.Verify(r => r.SalvarAlteracoesAsync(), Times.Once);

            // Garante que não chamou a B3 atoa
            _cotacaoB3Mock.Verify(s => s.ObterCotacoesFechamentoAsync(It.IsAny<DateTime>(), It.IsAny<List<string>>()), Times.Never);
        }

        [Fact(DisplayName = "Consultar Master - Deve calcular os resíduos com a cotação atualizada da B3")]
        public async Task ConsultarMaster_DeveCalcularResiduos_ComSucesso()
        {
            // Arrange
            var contaMaster = new ContaGrafica
            {
                Id = 1,
                NumeroConta = "MST-000001",
                Tipo = TipoConta.Master
            };

            contaMaster.Custodias.Add(new Custodia
            {
                Ticker = "WEGE3",
                Quantidade = 5,
                PrecoMedio = 30.00m,
                DataUltimaAtualizacao = new DateTime(2026, 2, 5)
            });

            _contaRepoMock.Setup(r => r.ObterContaMasterComCustodiaAsync())
                          .ReturnsAsync(contaMaster);

            // Simula que a ação subiu para R$ 40,00 na B3
            var cotacoesHoje = new Dictionary<string, decimal> { { "WEGE3", 40.00m } };
            _cotacaoB3Mock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<DateTime>(), It.IsAny<List<string>>()))
                          .ReturnsAsync(cotacoesHoje);

            // Act
            var resultado = await _service.ConsultarCustodiaMasterAsync();

            // Assert
            Assert.Equal("MST-000001", resultado.ContaMaster.NumeroConta);
            Assert.Single(resultado.Custodia);

            var itemCustodia = resultado.Custodia.First();
            Assert.Equal("WEGE3", itemCustodia.Ticker);
            Assert.Equal(5, itemCustodia.Quantidade);
            Assert.Equal(200.00m, itemCustodia.ValorAtual); // 5 ações * R$ 40,00 da B3
            Assert.Contains("2026-02-05", itemCustodia.Origem);

            Assert.Equal(200.00m, resultado.ValorTotalResiduo);
        }
    }
}