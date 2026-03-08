using Microsoft.AspNetCore.Mvc;
using Domain.Business;
using System;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RebalanceamentoController : ControllerBase
    {
        private readonly IRebalanceamentoService _rebalanceamentoService;

        public RebalanceamentoController(IRebalanceamentoService rebalanceamentoService)
        {
            _rebalanceamentoService = rebalanceamentoService;
        }

        /// <summary>
        /// Dispara o motor de rebalanceamento de carteiras (Simula a ação de um Worker)
        /// </summary>
        [HttpPost("executar")]
        public async Task<IActionResult> ExecutarRebalanceamento([FromQuery] string dataReferencia)
        {
            try
            {
                if (!DateTime.TryParse(dataReferencia, out var dataReal))
                {
                    return BadRequest(new
                    {
                        Erro = "Formato de data inválido.",
                        Dica = "Use o padrão yyyy-MM-dd (Ex: 2026-03-17)"
                    });
                }

                var resultado = await _rebalanceamentoService.ExecutarRebalanceamentoAsync(dataReal);

                return Ok(new
                {
                    Mensagem = resultado,
                    DataExecucao = dataReal
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Erro = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Erro = "Falha interna ao executar rebalanceamento.", Detalhe = ex.Message });
            }
        }
    }
}