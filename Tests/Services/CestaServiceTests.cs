using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Business;
using Domain.Dto.Admin;
using Domain.Entities;
using Domain.Repositories;
using Moq;
using Xunit;

namespace Tests.Services
{
    public class CestaServiceTests
    {
        private readonly Mock<ICestaRepository> _cestaRepoMock;
        private readonly CestaService _service;

        public CestaServiceTests()
        {
            _cestaRepoMock = new Mock<ICestaRepository>();
            _service = new CestaService(_cestaRepoMock.Object);
        }

        // 1. Cadastrar Cesta (Regras e Rebalanceamento)
        [Fact(DisplayName = "Cadastrar Cesta - Deve lançar exceção se não tiver exatamente 5 ativos")]
        public async Task CadastrarCesta_DeveLancarErro_QuandoQuantidadeAtivosInvalida()
        {
            // Arrange
            var request = new CriarCestaRequestDto
            {
                Nome = "Cesta Falha",
                Itens = new List<ItemCestaRequestDto>
                {
                    new ItemCestaRequestDto { Ticker = "PETR4", Percentual = 100m } // Só 1 ativo
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CadastrarCestaAsync(request));
            Assert.Contains("QUANTIDADE_ATIVOS_INVALIDA", ex.Message);
            Assert.Contains("exatamente 5 ativos", ex.Message);
        }

        [Fact(DisplayName = "Cadastrar Cesta - Deve lançar exceção se soma for diferente de 100%")]
        public async Task CadastrarCesta_DeveLancarErro_QuandoSomaDiferenteDe100()
        {
            // Arrange
            var request = new CriarCestaRequestDto
            {
                Nome = "Cesta Falha",
                Itens = new List<ItemCestaRequestDto>
                {
                    new ItemCestaRequestDto { Ticker = "A1", Percentual = 20m },
                    new ItemCestaRequestDto { Ticker = "A2", Percentual = 20m },
                    new ItemCestaRequestDto { Ticker = "A3", Percentual = 20m },
                    new ItemCestaRequestDto { Ticker = "A4", Percentual = 20m },
                    new ItemCestaRequestDto { Ticker = "A5", Percentual = 19m } // Soma 99%
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CadastrarCestaAsync(request));
            Assert.Contains("PERCENTUAIS_INVALIDOS", ex.Message);
            Assert.Contains("Soma atual: 99", ex.Message);
        }

        [Fact(DisplayName = "Cadastrar Cesta - Deve criar primeira cesta com sucesso sem rebalanceamento")]
        public async Task CadastrarCesta_PrimeiraCesta_DeveCriarSemRebalanceamento()
        {
            // Arrange
            var request = CriarRequestValido();

            // Simula que o banco não tem nenhuma cesta ativa
            _cestaRepoMock.Setup(r => r.ObterCestaAtualAsync()).ReturnsAsync((CestaRecomendacao)null);

            // Simula o ID gerado ao salvar
            _cestaRepoMock.Setup(r => r.AdicionarAsync(It.IsAny<CestaRecomendacao>()))
                          .Callback<CestaRecomendacao>(c => c.Id = 1)
                          .Returns(Task.CompletedTask);

            // Act
            var resultado = await _service.CadastrarCestaAsync(request);

            // Assert
            Assert.Equal(1, resultado.CestaId);
            Assert.True(resultado.Ativa);
            Assert.False(resultado.RebalanceamentoDisparado);
            Assert.Null(resultado.CestaAnteriorDesativada);
            _cestaRepoMock.Verify(r => r.SalvarAlteracoesAsync(), Times.Once);
        }

        [Fact(DisplayName = "Cadastrar Cesta - Deve desativar cesta anterior e detectar rebalanceamento")]
        public async Task CadastrarCesta_ComCestaAnterior_DeveDesativarEIdentificarRebalanceamento()
        {
            // Arrange
            var request = CriarRequestValido(); // Traz os ativos A1, A2, A3, A4 e A5

            var cestaAnterior = new CestaRecomendacao
            {
                Id = 99,
                Nome = "Cesta Velha",
                Ativa = true,
                Itens = new List<ItemCesta>
                {
                    new ItemCesta { Ticker = "A1" },
                    new ItemCesta { Ticker = "A2" },
                    new ItemCesta { Ticker = "A3" },
                    new ItemCesta { Ticker = "A4" },
                    new ItemCesta { Ticker = "B1" } // Ativo que será removido
                }
            };

            _cestaRepoMock.Setup(r => r.ObterCestaAtualAsync()).ReturnsAsync(cestaAnterior);

            // Act
            var resultado = await _service.CadastrarCestaAsync(request);

            // Assert
            Assert.True(resultado.RebalanceamentoDisparado);
            Assert.False(cestaAnterior.Ativa); // Garante que a entidade antiga foi inativada

            // Valida o cálculo matemático do Diff (Quem entrou e quem saiu)
            Assert.Contains("A5", resultado.AtivosAdicionados);
            Assert.DoesNotContain("B1", resultado.AtivosAdicionados);

            Assert.Contains("B1", resultado.AtivosRemovidos);
            Assert.DoesNotContain("A5", resultado.AtivosRemovidos);

            _cestaRepoMock.Verify(r => r.AtualizarAsync(cestaAnterior), Times.Once);
            _cestaRepoMock.Verify(r => r.AdicionarAsync(It.IsAny<CestaRecomendacao>()), Times.Once);
        }

        // 2. Consultas
        [Fact(DisplayName = "Consultar Cesta Atual - Deve retornar os dados corretamente")]
        public async Task ConsultarCestaAtual_DeveRetornarCesta()
        {
            // Arrange
            var cestaAtual = new CestaRecomendacao { Id = 1, Nome = "Cesta Ativa", Ativa = true };
            _cestaRepoMock.Setup(r => r.ObterCestaAtualAsync()).ReturnsAsync(cestaAtual);

            // Act
            var resultado = await _service.ConsultarCestaAtualAsync();

            // Assert
            Assert.Equal("Cesta Ativa", resultado.Nome);
        }

        [Fact(DisplayName = "Consultar Cesta Atual - Deve lançar erro se banco estiver vazio")]
        public async Task ConsultarCestaAtual_DeveLancarErro_QuandoNaoHouverCesta()
        {
            // Arrange
            _cestaRepoMock.Setup(r => r.ObterCestaAtualAsync()).ReturnsAsync((CestaRecomendacao)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ConsultarCestaAtualAsync());
            Assert.Contains("CESTA_NAO_ENCONTRADA", ex.Message);
        }

        [Fact(DisplayName = "Obter Histórico - Deve mapear a lista de cestas corretamente")]
        public async Task ObterHistoricoCestas_DeveRetornarLista()
        {
            // Arrange
            var historico = new List<CestaRecomendacao>
            {
                new CestaRecomendacao { Id = 1, Ativa = false },
                new CestaRecomendacao { Id = 2, Ativa = true }
            };
            _cestaRepoMock.Setup(r => r.ObterHistoricoAsync()).ReturnsAsync(historico);

            // Act
            var resultado = await _service.ObterHistoricoCestasAsync();

            // Assert
            Assert.Equal(2, resultado.Cestas.Count);
            Assert.Contains(resultado.Cestas, c => c.CestaId == 1 && !c.Ativa);
            Assert.Contains(resultado.Cestas, c => c.CestaId == 2 && c.Ativa);
        }

        private CriarCestaRequestDto CriarRequestValido()
        {
            return new CriarCestaRequestDto
            {
                Nome = "Cesta Top 5",
                Itens = new List<ItemCestaRequestDto>
                {
                    new ItemCestaRequestDto { Ticker = "A1", Percentual = 20m },
                    new ItemCestaRequestDto { Ticker = "A2", Percentual = 20m },
                    new ItemCestaRequestDto { Ticker = "A3", Percentual = 20m },
                    new ItemCestaRequestDto { Ticker = "A4", Percentual = 20m },
                    new ItemCestaRequestDto { Ticker = "A5", Percentual = 20m }
                }
            };
        }
    }
}