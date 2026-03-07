using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Business;
using Domain.Dto.Cliente;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProjectWebAPI.Controllers;
using Xunit;

namespace Tests.Controllers
{
    public class ClientesControllerTests
    {
        private readonly Mock<IClienteService> _clienteServiceMock;
        private readonly ClientesController _controller;

        public ClientesControllerTests()
        {
            _clienteServiceMock = new Mock<IClienteService>();
            _controller = new ClientesController(_clienteServiceMock.Object);
        }

        [Fact(DisplayName = "Aderir Produto - Deve retornar 201 Created no caminho feliz")]
        public async Task AderirProduto_DeveRetornar201Created_QuandoSucesso()
        {
            // Arrange
            var request = new AdesaoRequestDto { Nome = "Bruno", Cpf = "12345678900", ValorMensal = 1000m };
            var responseDto = new AdesaoResponseDto
            {
                ClienteId = 1,
                Nome = "Bruno",
                ContaGrafica = new ContaGraficaDto { NumeroConta = "FIL-123456" }
            };

            _clienteServiceMock.Setup(s => s.AderirProdutoAsync(request)).ReturnsAsync(responseDto);

            // Act
            var resultado = await _controller.AderirProduto(request);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(resultado);
            Assert.Equal("/api/clientes/1", createdResult.Location);

            var returnValue = Assert.IsType<AdesaoResponseDto>(createdResult.Value);
            Assert.Equal("FIL-123456", returnValue.ContaGrafica.NumeroConta);
        }

        [Fact(DisplayName = "Aderir Produto - Deve retornar 400 BadRequest em erro de negócio")]
        public async Task AderirProduto_DeveRetornar400_QuandoErroNegocio()
        {
            // Arrange
            var request = new AdesaoRequestDto();
            _clienteServiceMock.Setup(s => s.AderirProdutoAsync(request))
                               .ThrowsAsync(new InvalidOperationException("CPF_INVALIDO|O CPF informado já possui conta."));

            // Act
            var resultado = await _controller.AderirProduto(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(resultado);
            var erro = Assert.IsType<ErroResponseDto>(badRequestResult.Value);
            Assert.Equal("CPF_INVALIDO", erro.Codigo);
            Assert.Equal("O CPF informado já possui conta.", erro.Erro);
        }

        [Fact(DisplayName = "Sair Produto - Deve retornar 200 OK ao desativar conta")]
        public async Task SairProduto_DeveRetornar200OK_QuandoSucesso()
        {
            // Arrange
            var responseDto = new SaidaResponseDto { Mensagem = "Saída processada" };
            _clienteServiceMock.Setup(s => s.SairProdutoAsync(1)).ReturnsAsync(responseDto);

            // Act
            var resultado = await _controller.SairProduto(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            Assert.NotNull(okResult.Value);
        }

        [Fact(DisplayName = "Sair Produto - Deve retornar 404 NotFound se cliente não existir")]
        public async Task SairProduto_DeveRetornar404_QuandoClienteNaoEncontrado()
        {
            // Arrange
            _clienteServiceMock.Setup(s => s.SairProdutoAsync(99))
                               .ThrowsAsync(new InvalidOperationException("CLIENTE_NAO_ENCONTRADO|Cliente não localizado."));

            // Act
            var resultado = await _controller.SairProduto(99);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(resultado);
            var erro = Assert.IsType<ErroResponseDto>(notFoundResult.Value);
            Assert.Equal("CLIENTE_NAO_ENCONTRADO", erro.Codigo);
        }

        [Fact(DisplayName = "Alterar Valor Mensal - Deve retornar 200 OK")]
        public async Task AlterarValorMensal_DeveRetornar200OK()
        {
            // Arrange
            var request = new AlterarValorMensalRequestDto { NovoValorMensal = 5000m };
            var responseDto = new AlterarValorMensalResponseDto { Mensagem = "Valor alterado" };

            _clienteServiceMock.Setup(s => s.AlterarValorMensalAsync(1, request)).ReturnsAsync(responseDto);

            // Act
            var resultado = await _controller.AlterarValorMensal(1, request);

            // Assert
            Assert.IsType<OkObjectResult>(resultado);
        }

        [Fact(DisplayName = "Alterar Valor Mensal - Deve retornar 400 BadRequest para regra inválida")]
        public async Task AlterarValorMensal_DeveRetornar400_QuandoRegraInvalida()
        {
            // Arrange
            var request = new AlterarValorMensalRequestDto { NovoValorMensal = -100m };
            _clienteServiceMock.Setup(s => s.AlterarValorMensalAsync(1, request))
                               .ThrowsAsync(new InvalidOperationException("VALOR_INVALIDO|O valor deve ser maior que zero."));

            // Act
            var resultado = await _controller.AlterarValorMensal(1, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(resultado);
            var erro = Assert.IsType<ErroResponseDto>(badRequestResult.Value);
            Assert.Equal("VALOR_INVALIDO", erro.Codigo);
        }

        [Fact(DisplayName = "Consultar Carteira - Deve retornar 200 OK com resumo")]
        public async Task ConsultarCarteira_DeveRetornar200OK()
        {
            // Arrange
            var responseDto = new ConsultaCarteiraResponseDto { ClienteId = 1 };
            _clienteServiceMock.Setup(s => s.ConsultarCarteiraAsync(1)).ReturnsAsync(responseDto);

            // Act
            var resultado = await _controller.ConsultarCarteira(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            Assert.IsType<ConsultaCarteiraResponseDto>(okResult.Value);
        }

        [Fact(DisplayName = "Consultar Carteira - Deve retornar 404 NotFound se erro")]
        public async Task ConsultarCarteira_DeveRetornar404_QuandoErro()
        {
            // Arrange
            _clienteServiceMock.Setup(s => s.ConsultarCarteiraAsync(99))
                               .ThrowsAsync(new InvalidOperationException("ERRO|Falha."));

            // Act
            var resultado = await _controller.ConsultarCarteira(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(resultado);
        }

        [Fact(DisplayName = "Consultar Rentabilidade - Deve retornar 200 OK")]
        public async Task ConsultarRentabilidade_DeveRetornar200OK()
        {
            // Arrange
            var responseDto = new RentabilidadeDetalhadaResponseDto { ClienteId = 1 };
            _clienteServiceMock.Setup(s => s.ConsultarRentabilidadeDetalhadaAsync(1)).ReturnsAsync(responseDto);

            // Act
            var resultado = await _controller.ConsultarRentabilidadeDetalhada(1);

            // Assert
            Assert.IsType<OkObjectResult>(resultado);
        }
    }
}