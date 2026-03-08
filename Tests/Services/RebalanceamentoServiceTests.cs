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
    public class RebalanceamentoServiceTests
    {
        private readonly Mock<IClienteRepository> _clienteRepositoryMock;
        private readonly Mock<ICestaRepository> _cestaRepositoryMock;
        private readonly Mock<ICotacaoB3Service> _cotacaoB3ServiceMock;
        private readonly Mock<IKafkaProducerService> _kafkaProducerServiceMock;
        private readonly Mock<IContaRepository> _contaRepositoryMock;
        private readonly Mock<IRebalanceamentoRepository> _rebalanceamentoRepositoryMock;
        private readonly RebalanceamentoService _rebalanceamentoService;

        public RebalanceamentoServiceTests()
        {
            _clienteRepositoryMock = new Mock<IClienteRepository>();
            _cestaRepositoryMock = new Mock<ICestaRepository>();
            _cotacaoB3ServiceMock = new Mock<ICotacaoB3Service>();
            _kafkaProducerServiceMock = new Mock<IKafkaProducerService>();
            _contaRepositoryMock = new Mock<IContaRepository>();
            _rebalanceamentoRepositoryMock = new Mock<IRebalanceamentoRepository>();

            _rebalanceamentoService = new RebalanceamentoService(
                _clienteRepositoryMock.Object,
                _cestaRepositoryMock.Object,
                _cotacaoB3ServiceMock.Object,
                _kafkaProducerServiceMock.Object,
                _contaRepositoryMock.Object,
                _rebalanceamentoRepositoryMock.Object
            );
        }

        [Fact]
        public async Task ExecutarRebalanceamento_SemCestaAtiva_DeveLancarExcecao()
        {
            // Arrange
            _cestaRepositoryMock.Setup(r => r.ObterCestaAtualAsync()).ReturnsAsync((CestaRecomendacao)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _rebalanceamentoService.ExecutarRebalanceamentoAsync(DateTime.UtcNow));

            Assert.Contains("Nenhuma cesta ativa encontrada", ex.Message);
        }

        [Fact]
        public async Task ExecutarRebalanceamento_SemClientesAtivos_DeveRetornarMensagem()
        {
            // Arrange
            _cestaRepositoryMock.Setup(r => r.ObterCestaAtualAsync()).ReturnsAsync(new CestaRecomendacao());
            _clienteRepositoryMock.Setup(r => r.ObterClientesAtivosComCustodiaAsync()).ReturnsAsync(new List<Cliente>());

            // Act
            var resultado = await _rebalanceamentoService.ExecutarRebalanceamentoAsync(DateTime.UtcNow);

            // Assert
            Assert.Equal("Nenhum cliente ativo para rebalancear.", resultado);
        }

        [Fact]
        public async Task ExecutarRebalanceamento_VendaAbaixoDe20Mil_NaoDeveEnviarIRParaKafka()
        {
            // Arrange
            var dataReferencia = new DateTime(2026, 3, 17);

            // Cesta NOVA só tem VALE3
            var cesta = new CestaRecomendacao { Itens = new List<ItemCesta> { new ItemCesta { Ticker = "VALE3", Percentual = 100m } } };

            // Cliente tem WEGE3 (vai precisar vender)
            var cliente = new Cliente
            {
                Id = 1,
                ContasGraficas = new List<ContaGrafica>
                {
                    new ContaGrafica
                    {
                        Tipo = TipoConta.Filhote,
                        Custodias = new List<Custodia> { new Custodia { Ticker = "WEGE3", Quantidade = 100, PrecoMedio = 40m } }
                    }
                }
            };

            _cestaRepositoryMock.Setup(r => r.ObterCestaAtualAsync()).ReturnsAsync(cesta);
            _clienteRepositoryMock.Setup(r => r.ObterClientesAtivosComCustodiaAsync()).ReturnsAsync(new List<Cliente> { cliente });

            // Cliente não vendeu nada no mês ainda
            _clienteRepositoryMock.Setup(r => r.ObterTotalVendasMesAsync(1, 3, 2026)).ReturnsAsync(0m);

            var cotacoes = new Dictionary<string, decimal> { { "WEGE3", 50m }, { "VALE3", 100m } };
            _cotacaoB3ServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<DateTime>(), It.IsAny<List<string>>())).ReturnsAsync(cotacoes);

            // Act
            // Vai vender 100 WEGE3 a R$50 = R$ 5.000 (Lucro de 1000). Como 5.000 < 20.000, NÃO tem IR!
            await _rebalanceamentoService.ExecutarRebalanceamentoAsync(dataReferencia);

            // Assert
            _kafkaProducerServiceMock.Verify(
                k => k.PublicarEventoIRAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<string>()),
                Times.Never); // GARANTE QUE NÃO COBROU IMPOSTO INDEVIDO

            _rebalanceamentoRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Rebalanceamento>()), Times.Exactly(2)); // 1 Venda + 1 Compra
            _contaRepositoryMock.Verify(r => r.SalvarAlteracoesAsync(), Times.Once);
        }

        [Fact]
        public async Task ExecutarRebalanceamento_VendaAcimaDe20Mil_ComLucro_DeveEnviarIRParaKafka()
        {
            // Arrange
            var dataReferencia = new DateTime(2026, 3, 17);
            var cesta = new CestaRecomendacao { Itens = new List<ItemCesta> { new ItemCesta { Ticker = "VALE3", Percentual = 100m } } };

            var cliente = new Cliente
            {
                Id = 1,
                ContasGraficas = new List<ContaGrafica>
                {
                    new ContaGrafica
                    {
                        Tipo = TipoConta.Filhote,
                        Custodias = new List<Custodia> { new Custodia { Ticker = "WEGE3", Quantidade = 1000, PrecoMedio = 40m } }
                    }
                }
            };

            _cestaRepositoryMock.Setup(r => r.ObterCestaAtualAsync()).ReturnsAsync(cesta);
            _clienteRepositoryMock.Setup(r => r.ObterClientesAtivosComCustodiaAsync()).ReturnsAsync(new List<Cliente> { cliente });

            // O cliente JÁ TINHA vendido 15 mil esse mês
            _clienteRepositoryMock.Setup(r => r.ObterTotalVendasMesAsync(1, 3, 2026)).ReturnsAsync(15000m);

            var cotacoes = new Dictionary<string, decimal> { { "WEGE3", 50m }, { "VALE3", 100m } };
            _cotacaoB3ServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<DateTime>(), It.IsAny<List<string>>())).ReturnsAsync(cotacoes);

            // Act
            // Vai vender 1000 WEGE3 a R$50 = R$ 50.000. 
            // Total do mês = 15k + 50k = 65.000 (Passou de 20k!)
            // Lucro = (50 - 40) * 1000 = 10.000 de Lucro.
            // IR 20% sobre 10.000 = R$ 2.000,00.
            await _rebalanceamentoService.ExecutarRebalanceamentoAsync(dataReferencia);

            // Assert
            _kafkaProducerServiceMock.Verify(
                k => k.PublicarEventoIRAsync(
                    1,
                    "IR_VENDA_REBALANCEAMENTO",
                    10000m, // Lucro
                    2000m,  // Imposto de 20%
                    "2026-03-17"),
                Times.Once); // GARANTE QUE COBROU EXATAMENTE 1 VEZ NO KAFKA
        }
    }
}