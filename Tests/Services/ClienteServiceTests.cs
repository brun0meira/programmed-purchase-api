using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Business;
using Domain.Dto.Cliente;
using Domain.Entities;
using Domain.Enums;
using Domain.ExternalServices;
using Domain.Repositories;
using Moq;
using Xunit;

namespace Tests.Services
{
    public class ClienteServiceTests
    {
        private readonly Mock<IClienteRepository> _clienteRepoMock;
        private readonly Mock<ICotacaoB3Service> _cotacaoB3Mock;
        private readonly ClienteService _service;

        public ClienteServiceTests()
        {
            _clienteRepoMock = new Mock<IClienteRepository>();
            _cotacaoB3Mock = new Mock<ICotacaoB3Service>();

            _service = new ClienteService(_clienteRepoMock.Object, _cotacaoB3Mock.Object);
        }

        // 1. Aderir Produto
        [Fact(DisplayName = "Aderir Produto - Deve criar cliente e conta gráfica com sucesso")]
        public async Task AderirProduto_DeveCriarClienteEConta_ComSucesso()
        {
            // Arrange
            var request = new AdesaoRequestDto
            {
                Nome = "Bruno",
                Cpf = "123",
                Email = "b@b.com",
                ValorMensal = 1000m
            };

            _clienteRepoMock.Setup(r => r.CpfExisteAsync("123")).ReturnsAsync(false);

            // Simula o ID gerado ao adicionar
            _clienteRepoMock.Setup(r => r.AdicionarAsync(It.IsAny<Cliente>()))
                            .Callback<Cliente>(c => c.Id = 1)
                            .Returns(Task.CompletedTask);

            // Act
            var resultado = await _service.AderirProdutoAsync(request);

            // Assert
            Assert.Equal(1, resultado.ClienteId);
            Assert.Equal("Bruno", resultado.Nome);
            Assert.True(resultado.Ativo);
            Assert.NotNull(resultado.ContaGrafica);
            Assert.StartsWith("FLH-", resultado.ContaGrafica.NumeroConta);

            _clienteRepoMock.Verify(r => r.SalvarAlteracoesAsync(), Times.Once);
        }

        [Fact(DisplayName = "Aderir Produto - Deve estourar erro se CPF for duplicado")]
        public async Task AderirProduto_DeveLancarErro_QuandoCpfDuplicado()
        {
            // Arrange
            var request = new AdesaoRequestDto { Cpf = "123" };
            _clienteRepoMock.Setup(r => r.CpfExisteAsync("123")).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AderirProdutoAsync(request));
            Assert.Contains("CLIENTE_CPF_DUPLICADO", ex.Message);
        }

        // 2. Sair
        [Fact(DisplayName = "Sair Produto - Deve inativar cliente com sucesso")]
        public async Task SairProduto_DeveInativarCliente_ComSucesso()
        {
            // Arrange
            var cliente = new Cliente { Id = 1, Ativo = true };
            _clienteRepoMock.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cliente);

            // Act
            var resultado = await _service.SairProdutoAsync(1);

            // Assert
            Assert.False(resultado.Ativo); // Confirma que o DTO voltou falso
            Assert.False(cliente.Ativo);   // Confirma que a entidade foi alterada
            _clienteRepoMock.Verify(r => r.AtualizarAsync(cliente), Times.Once);
            _clienteRepoMock.Verify(r => r.SalvarAlteracoesAsync(), Times.Once);
        }

        [Fact(DisplayName = "Sair Produto - Deve lançar erro se cliente já estiver inativo")]
        public async Task SairProduto_DeveLancarErro_QuandoClienteJaInativo()
        {
            // Arrange
            var cliente = new Cliente { Id = 1, Ativo = false };
            _clienteRepoMock.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cliente);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SairProdutoAsync(1));
            Assert.Contains("CLIENTE_JA_INATIVO", ex.Message);
        }

        // 3. O cálculo pesado de rentabilidade da carteira
        [Fact(DisplayName = "Consultar Carteira - Deve calcular rentabilidade (PL) corretamente")]
        public async Task ConsultarCarteira_DeveCalcularRentabilidade_ComSucesso()
        {
            // Arrange
            var clienteId = 1;
            var cliente = new Cliente { Id = clienteId, Nome = "João" };

            var conta = new ContaGrafica { Tipo = TipoConta.Filhote, NumeroConta = "FLH-TESTE" };
            conta.Custodias.Add(new Custodia
            {
                Ticker = "PETR4",
                Quantidade = 100,
                PrecoMedio = 20.00m // Investiu R$ 2000
            });
            cliente.ContasGraficas.Add(conta);

            _clienteRepoMock.Setup(r => r.ObterClienteComCustodiaAsync(clienteId))
                            .ReturnsAsync(cliente);

            // Simula que hoje a PETR4 está valendo R$ 25,00 (Lucro de R$ 5 por ação)
            var cotacoesHoje = new Dictionary<string, decimal> { { "PETR4", 25.00m } };
            _cotacaoB3Mock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<DateTime>(), It.IsAny<List<string>>()))
                          .ReturnsAsync(cotacoesHoje);

            // Act
            var resultado = await _service.ConsultarCarteiraAsync(clienteId);

            // Assert
            Assert.Equal(2000m, resultado.Resumo.ValorTotalInvestido);
            Assert.Equal(2500m, resultado.Resumo.ValorAtualCarteira);
            Assert.Equal(500m, resultado.Resumo.PlTotal);
            Assert.Equal(25.00m, resultado.Resumo.RentabilidadePercentual); // (500 / 2000) * 100

            var ativo = resultado.Ativos[0];
            Assert.Equal("PETR4", ativo.Ticker);
            Assert.Equal(25.00m, ativo.CotacaoAtual);
            Assert.Equal(500m, ativo.Pl);
            Assert.Equal(100m, ativo.ComposicaoCarteira); // 100% da carteira é PETR4
        }

        [Fact(DisplayName = "Consultar Carteira - Deve retornar zerado se o cliente não tiver custódia")]
        public async Task ConsultarCarteira_DeveRetornarZerado_QuandoSemCustodia()
        {
            // Arrange
            var cliente = new Cliente { Id = 1 };
            // Cliente sem conta gráfica
            _clienteRepoMock.Setup(r => r.ObterClienteComCustodiaAsync(1)).ReturnsAsync(cliente);

            // Act
            var resultado = await _service.ConsultarCarteiraAsync(1);

            // Assert
            Assert.Equal(0, resultado.Resumo.ValorTotalInvestido);
            Assert.Empty(resultado.Ativos);
            // Garante que o serviço de cotação não foi chamado
            _cotacaoB3Mock.Verify(s => s.ObterCotacoesFechamentoAsync(It.IsAny<DateTime>(), It.IsAny<List<string>>()), Times.Never);
        }
    }
}