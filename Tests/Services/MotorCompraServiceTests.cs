using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Business;
using Domain.Dto.Motor;
using Domain.Entities;
using Domain.Enums;
using Domain.ExternalServices;
using Domain.Repositories;
using Moq;
using Xunit;

namespace Tests.Services
{
    public class MotorCompraServiceTests
    {
        private readonly Mock<IClienteRepository> _clienteRepoMock;
        private readonly Mock<IContaRepository> _contaRepoMock;
        private readonly Mock<ICestaRepository> _cestaRepoMock;
        private readonly Mock<ICotacaoB3Service> _cotacaoB3Mock;
        private readonly Mock<IKafkaProducerService> _kafkaMock;
        private readonly MotorCompraService _service;

        public MotorCompraServiceTests()
        {
            _clienteRepoMock = new Mock<IClienteRepository>();
            _contaRepoMock = new Mock<IContaRepository>();
            _cestaRepoMock = new Mock<ICestaRepository>();
            _cotacaoB3Mock = new Mock<ICotacaoB3Service>();
            _kafkaMock = new Mock<IKafkaProducerService>();

            _service = new MotorCompraService(
                _clienteRepoMock.Object,
                _contaRepoMock.Object,
                _cestaRepoMock.Object,
                _cotacaoB3Mock.Object,
                _kafkaMock.Object
            );
        }

        [Fact(DisplayName = "Executar Compra - Deve calcular rateio, separar lotes, sobrar resíduo e chamar o Kafka")]
        public async Task ExecutarCompra_CenarioCompleto_Sucesso()
        {
            // Preparação do cenário de teste:
            var dataRef = "2026-02-05";
            var request = new ExecutarCompraRequestDto { DataReferencia = dataRef };

            // Criando Clientes:
            // Cliente 1: Valor 6000 (Aporte = 2000)
            // Cliente 2: Valor 3000 (Aporte = 1000)
            // Total Consolidado = 3000
            var cliente1 = new Cliente { Id = 1, Nome = "Cliente Maior", ValorMensal = 6000m };
            cliente1.ContasGraficas.Add(new ContaGrafica { Tipo = TipoConta.Filhote });

            var cliente2 = new Cliente { Id = 2, Nome = "Cliente Menor", ValorMensal = 3000m };
            cliente2.ContasGraficas.Add(new ContaGrafica { Tipo = TipoConta.Filhote });

            _clienteRepoMock.Setup(r => r.ObterClientesAtivosComCustodiaAsync())
                            .ReturnsAsync(new List<Cliente> { cliente1, cliente2 });

            // Criando Cesta (100% em PETR4)
            var cesta = new CestaRecomendacao
            {
                Itens = new List<ItemCesta> { new ItemCesta { Ticker = "PETR4", Percentual = 100m } }
            };
            _cestaRepoMock.Setup(r => r.ObterCestaAtualAsync()).ReturnsAsync(cesta);

            // Criando Conta Master vazia
            var contaMaster = new ContaGrafica { Tipo = TipoConta.Master, NumeroConta = "MST-001" };
            _contaRepoMock.Setup(r => r.ObterContaMasterComCustodiaAsync()).ReturnsAsync(contaMaster);

            // Simula a Cotação (PETR4 a R$ 15,00)
            // Matemática: 3000 / 15 = 200 ações para comprar no mercado.
            // Cliente 1 (2/3) = 133 ações
            // Cliente 2 (1/3) = 66 ações
            // Total Distribuído = 199 ações. Sobra na Master = 1 ação.
            var cotacoes = new Dictionary<string, decimal> { { "PETR4", 15.00m } };
            _cotacaoB3Mock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<DateTime>(), It.IsAny<List<string>>()))
                          .ReturnsAsync(cotacoes);

            // 2. Ação
            var resultado = await _service.ExecutarCompraProgramadaAsync(request);

            // 3. Validações
            // A. Validações Gerais
            Assert.Equal(3000m, resultado.TotalConsolidado);
            Assert.Equal(2, resultado.TotalClientes);

            // B. Validação da Ordem de Compra (Deve comprar 200 ações)
            var ordem = resultado.OrdensCompra.First();
            Assert.Equal(200, ordem.QuantidadeTotal);
            // Como 200 é múltiplo de 100, deve ter criado apenas Lote Padrão e nenhum Fracionário
            Assert.Contains(ordem.Detalhes, d => d.Tipo == "LOTE_PADRAO" && d.Quantidade == 200);
            Assert.DoesNotContain(ordem.Detalhes, d => d.Tipo == "FRACIONARIO");

            // C. Validação do Rateio
            var distCliente1 = resultado.Distribuicoes.First(d => d.ClienteId == 1);
            var distCliente2 = resultado.Distribuicoes.First(d => d.ClienteId == 2);

            Assert.Equal(133, distCliente1.Ativos.First(a => a.Ticker == "PETR4").Quantidade);
            Assert.Equal(66, distCliente2.Ativos.First(a => a.Ticker == "PETR4").Quantidade);

            // D. Validação do Resíduo (1 ação que sobrou das frações)
            var residuo = resultado.ResiduosCustMaster.First();
            Assert.Equal("PETR4", residuo.Ticker);
            Assert.Equal(1, residuo.Quantidade);

            // E. Validação do Kafka (Garante que disparou o evento de Dedo-Duro para os 2 clientes)
            _kafkaMock.Verify(k => k.PublicarEventoIRAsync(
                1, "DEDO_DURO", 1995m /* 133*15 */, It.IsAny<decimal>(), dataRef), Times.Once);

            _kafkaMock.Verify(k => k.PublicarEventoIRAsync(
                2, "DEDO_DURO", 990m /* 66*15 */, It.IsAny<decimal>(), dataRef), Times.Once);

            // F. Valida se salvou no banco
            _contaRepoMock.Verify(r => r.SalvarAlteracoesAsync(), Times.Once);
        }

        [Fact(DisplayName = "Executar Compra - Deve lançar exceção se data for inválida")]
        public async Task ExecutarCompra_DataInvalida_LancaExcecao()
        {
            var request = new ExecutarCompraRequestDto { DataReferencia = "DATA-ERRADA" };

            var excecao = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ExecutarCompraProgramadaAsync(request)
            );
            Assert.Contains("DATA_INVALIDA", excecao.Message);
        }
    }
}