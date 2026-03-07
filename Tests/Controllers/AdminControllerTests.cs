using System;
using System.Threading.Tasks;
using Domain.Business;
using Domain.Dto.Admin;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProjectWebAPI.Controllers;
using Xunit;

namespace Tests.Controllers
{
    public class AdminControllerTests
    {
        // Mocks
        private readonly Mock<ICestaService> _cestaServiceMock;
        private readonly Mock<IContaMasterService> _contaMasterServiceMock;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            _cestaServiceMock = new Mock<ICestaService>();
            _contaMasterServiceMock = new Mock<IContaMasterService>();

            _controller = new AdminController(_cestaServiceMock.Object, _contaMasterServiceMock.Object);
        }

        [Fact(DisplayName = "Obter Cesta Atual - Deve retornar 200 OK quando existir cesta ativa")]
        public async Task ObterCestaAtual_DeveRetornar200OK_QuandoCestaExistir()
        {
            // Arrange
            var cestaMock = new CestaResponseDto
            {
                CestaId = 1,
                Nome = "Cesta Top Five Mock",
                Ativa = true
            };

            _cestaServiceMock.Setup(s => s.ConsultarCestaAtualAsync())
                             .ReturnsAsync(cestaMock);

            // Act
            var resultado = await _controller.ObterCestaAtual();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado); // Garante um StatusCode 200
            var returnValue = Assert.IsType<CestaResponseDto>(okResult.Value); // Garante que o corpo da resposta é o DTO
            Assert.Equal("Cesta Top Five Mock", returnValue.Nome); // Garante que o nome bate 
        }

        [Fact(DisplayName = "Obter Cesta Atual - Deve retornar 404 NotFound quando não existir cesta")]
        public async Task ObterCestaAtual_DeveRetornar404NotFound_QuandoNaoExistirCesta()
        {
            // Arrange
            _cestaServiceMock.Setup(s => s.ConsultarCestaAtualAsync())
                             .ThrowsAsync(new InvalidOperationException("CESTA_NAO_ENCONTRADA|Nenhuma cesta ativa encontrada."));

            // Act
            var resultado = await _controller.ObterCestaAtual();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(resultado);

            var errorValue = notFoundResult.Value;
            var codigoProp = errorValue.GetType().GetProperty("Codigo").GetValue(errorValue, null);

            Assert.Equal("CESTA_NAO_ENCONTRADA", codigoProp);
        }

        [Fact(DisplayName = "Cadastrar Cesta - Deve retornar 201 Created quando o payload for válido")]
        public async Task CadastrarCesta_DeveRetornar201Created_QuandoPayloadValido()
        {
            // Arrange
            var request = new CriarCestaRequestDto { Nome = "Cesta Teste 100%" };
            var responseDto = new CestaResponseDto { CestaId = 1, Nome = "Cesta Teste 100%" };

            _cestaServiceMock.Setup(s => s.CadastrarCestaAsync(It.IsAny<CriarCestaRequestDto>()))
                             .ReturnsAsync(responseDto);

            // Act
            var resultado = await _controller.CadastrarCesta(request);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(resultado);
            Assert.Equal("/api/admin/cesta/atual", createdResult.Location);

            var returnValue = Assert.IsType<CestaResponseDto>(createdResult.Value);
            Assert.Equal("Cesta Teste 100%", returnValue.Nome);
        }

        [Fact(DisplayName = "Cadastrar Cesta - Deve retornar 400 BadRequest quando a soma não for 100%")]
        public async Task CadastrarCesta_DeveRetornar400BadRequest_QuandoSomaInvalida()
        {
            // Arrange
            var request = new CriarCestaRequestDto();
            _cestaServiceMock.Setup(s => s.CadastrarCestaAsync(It.IsAny<CriarCestaRequestDto>()))
                             .ThrowsAsync(new InvalidOperationException("PERCENTUAIS_INVALIDOS|A soma dos percentuais deve ser exatamente 100%."));

            // Act
            var resultado = await _controller.CadastrarCesta(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(resultado);
            var errorValue = badRequestResult.Value;
            var codigoProp = errorValue.GetType().GetProperty("Codigo").GetValue(errorValue, null);
            var erroProp = errorValue.GetType().GetProperty("Erro").GetValue(errorValue, null);

            Assert.Equal("PERCENTUAIS_INVALIDOS", codigoProp);
            Assert.Contains("soma dos percentuais", erroProp.ToString());
        }

        [Fact(DisplayName = "Obter Histórico - Deve retornar 200 OK com a lista de cestas")]
        public async Task ObterHistorico_DeveRetornar200OK_ComListaDeCestas()
        {
            // Arrange
            var responseDto = new HistoricoCestasResponseDto();
            
            responseDto.Cestas.Add(new CestaResponseDto { CestaId = 1, Nome = "Cesta Antiga" });

            _cestaServiceMock.Setup(s => s.ObterHistoricoCestasAsync())
                             .ReturnsAsync(responseDto);

            // Act
            var resultado = await _controller.ObterHistoricoCestas();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            var returnValue = Assert.IsType<HistoricoCestasResponseDto>(okResult.Value);
            Assert.Single(returnValue.Cestas);
        }

        [Fact(DisplayName = "Obter Custodia Master - Deve retornar 200 OK com os resíduos")]
        public async Task ObterCustodiaMaster_DeveRetornar200OK_ComResiduos()
        {
            // Arrange
            var responseDto = new CustodiaMasterResponseDto
            {
                ContaMaster = new ContaMasterDto { NumeroConta = "MST-000001" },
                ValorTotalResiduo = 150.50m
            };

            _contaMasterServiceMock.Setup(s => s.ConsultarCustodiaMasterAsync())
                                   .ReturnsAsync(responseDto);

            // Act
            var resultado = await _controller.ObterCustodiaMaster();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            var returnValue = Assert.IsType<CustodiaMasterResponseDto>(okResult.Value);
            Assert.Equal("MST-000001", returnValue.ContaMaster.NumeroConta);
            Assert.Equal(150.50m, returnValue.ValorTotalResiduo);
        }
    }
}