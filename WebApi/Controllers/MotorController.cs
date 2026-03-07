using System;
using System.Threading.Tasks;
using Domain.Business;
using Domain.Dto.Cliente;
using Domain.Dto.Motor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ProjectWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class MotorController : ControllerBase
    {
        private readonly IMotorCompraService _motorCompraService;

        public MotorController(IMotorCompraService motorCompraService)
        {
            _motorCompraService = motorCompraService;
        }

        /// <summary>
        /// Executa o motor de compra programada manualmente (simulação dos dias 5, 15 e 25).
        /// </summary>
        [HttpPost("executar-compra")]
        [ProducesResponseType(typeof(ExecutarCompraResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErroResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErroResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExecutarCompra([FromBody] ExecutarCompraRequestDto request)
        {
            try
            {
                var response = await _motorCompraService.ExecutarCompraProgramadaAsync(request);
                return Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("|"))
            {
                var partes = ex.Message.Split('|');
                if (partes[0] == "COTACAO_NAO_ENCONTRADA" || partes[0] == "CESTA_NAO_ENCONTRADA")
                    return NotFound(new ErroResponseDto { Codigo = partes[0], Erro = partes[1] });

                return BadRequest(new ErroResponseDto { Codigo = partes[0], Erro = partes[1] });
            }
        }
    }
}