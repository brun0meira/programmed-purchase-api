using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Business;
using Domain.Dto.Cliente;
using Domain.Dto.Motor;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProjectWebAPI.Controllers;
using Xunit;

namespace Tests.Controllers
{
    public class MotorControllerTests
    {
        private readonly Mock<IMotorCompraService> _motorCompraServiceMock;
        private readonly MotorController _controller;

        public MotorControllerTests()
        {
            _motorCompraServiceMock = new Mock<IMotorCompraService>();
            _controller = new MotorController(_motorCompraServiceMock.Object);
        }

        [Fact(DisplayName = "Executar Compra - Deve retornar 200 OK com o relatório de execução")]
        public async Task ExecutarCompra_DeveRetornar200OK_ComRelatorioCompleto()
        {
            // Arrange
            var request = new ExecutarCompraRequestDto { DataReferencia = "2026-02-05" };

            var responseDto = new ExecutarCompraResponseDto
            {
                DataExecucao = new DateTime(2026, 2, 5),
                TotalConsolidado = 15000.50m,
                OrdensCompra = new List<OrdemCompraResponseDto>
                {
                    new OrdemCompraResponseDto
                    {
                        Ticker = "PETR4",
                        QuantidadeTotal = 350,
                        PrecoUnitario = 42.85m,
                        ValorTotal = 14997.50m,
                        Detalhes = new List<OrdemDetalheDto>
                        {
                            new OrdemDetalheDto { Tipo = "LOTE_PADRAO", Ticker = "PETR4", Quantidade = 300 },
                            new OrdemDetalheDto { Tipo = "FRACIONARIO", Ticker = "PETR4F", Quantidade = 50 }
                        }
                    }
                }
            };

            _motorCompraServiceMock.Setup(s => s.ExecutarCompraProgramadaAsync(request))
                                   .ReturnsAsync(responseDto);

            // Act
            var resultado = await _controller.ExecutarCompra(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            var returnValue = Assert.IsType<ExecutarCompraResponseDto>(okResult.Value);

            // Valida contra o DateTime real
            Assert.Equal(new DateTime(2026, 2, 5), returnValue.DataExecucao);
            Assert.Equal(15000.50m, returnValue.TotalConsolidado);
            Assert.Single(returnValue.OrdensCompra); 
        }

        [Fact(DisplayName = "Executar Compra - Deve retornar 404 NotFound se o arquivo COTAHIST não existir")]
        public async Task ExecutarCompra_DeveRetornar404_QuandoArquivoCotacaoNaoExistir()
        {
            // Arrange
            var request = new ExecutarCompraRequestDto { DataReferencia = "2026-02-05" };

            _motorCompraServiceMock.Setup(s => s.ExecutarCompraProgramadaAsync(request))
                                   .ThrowsAsync(new InvalidOperationException("COTACAO_NAO_ENCONTRADA|Arquivo não encontrado para a data."));

            // Act
            var resultado = await _controller.ExecutarCompra(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(resultado);
            var erro = Assert.IsType<ErroResponseDto>(notFoundResult.Value);

            Assert.Equal("COTACAO_NAO_ENCONTRADA", erro.Codigo);
            Assert.Equal("Arquivo não encontrado para a data.", erro.Erro);
        }

        [Fact(DisplayName = "Executar Compra - Deve retornar 404 NotFound se não houver cesta ativa")]
        public async Task ExecutarCompra_DeveRetornar404_QuandoNaoHouverCestaAtiva()
        {
            // Arrange
            var request = new ExecutarCompraRequestDto { DataReferencia = "2026-02-05" };

            _motorCompraServiceMock.Setup(s => s.ExecutarCompraProgramadaAsync(request))
                                   .ThrowsAsync(new InvalidOperationException("CESTA_NAO_ENCONTRADA|Nenhuma cesta ativa."));

            // Act
            var resultado = await _controller.ExecutarCompra(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(resultado);
            var erro = Assert.IsType<ErroResponseDto>(notFoundResult.Value);

            Assert.Equal("CESTA_NAO_ENCONTRADA", erro.Codigo);
        }

        [Fact(DisplayName = "Executar Compra - Deve retornar 400 BadRequest para outros erros de negócio (ex: sem saldo)")]
        public async Task ExecutarCompra_DeveRetornar400_QuandoHouverErroDeNegocio()
        {
            // Arrange
            var request = new ExecutarCompraRequestDto { DataReferencia = "2026-02-05" };

            // Simula um erro genérico
            _motorCompraServiceMock.Setup(s => s.ExecutarCompraProgramadaAsync(request))
                                   .ThrowsAsync(new InvalidOperationException("SEM_CLIENTES_ATIVOS|Não há clientes para executar a compra."));

            // Act
            var resultado = await _controller.ExecutarCompra(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(resultado);
            var erro = Assert.IsType<ErroResponseDto>(badRequestResult.Value);

            Assert.Equal("SEM_CLIENTES_ATIVOS", erro.Codigo);
        }
    }
}