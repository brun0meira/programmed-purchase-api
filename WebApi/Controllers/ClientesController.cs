using Domain.Business;
using Domain.Dto.Cliente;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace ProjectWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteService _clienteService;

        public ClientesController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        /// <summary>
        /// Realiza a adesão do cliente ao produto de Compra Programada.
        /// </summary>
        /// <param name="request">Dados do cliente e valor mensal de aporte.</param>
        /// <returns>Dados do cliente e da conta gráfica criada.</returns>
        [HttpPost("adesao")]
        [ProducesResponseType(typeof(AdesaoResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErroResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AderirProduto([FromBody] AdesaoRequestDto request)
        {
            try
            {
                var response = await _clienteService.AderirProdutoAsync(request);
                return Created($"/api/clientes/{response.ClienteId}", response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("|"))
            {
                // Tratamento do erro lançado pelo Business
                var partes = ex.Message.Split('|');
                var erroResponse = new ErroResponseDto { Codigo = partes[0], Erro = partes[1] };
                return BadRequest(erroResponse);
            }
        }

        /// <summary>
        /// Solicita a saída do cliente do produto de Compra Programada.
        /// </summary>
        /// <param name="clienteId">ID do cliente.</param>
        /// <returns>Confirmação de saída com manutenção da custódia.</returns>
        [HttpPost("{clienteId}/saida")]
        [ProducesResponseType(typeof(SaidaResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErroResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErroResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SairProduto(long clienteId)
        {
            try
            {
                var response = await _clienteService.SairProdutoAsync(clienteId);
                return Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("|"))
            {
                var partes = ex.Message.Split('|');
                var erroResponse = new ErroResponseDto { Codigo = partes[0], Erro = partes[1] };

                if (partes[0] == "CLIENTE_NAO_ENCONTRADO")
                    return NotFound(erroResponse);

                return BadRequest(erroResponse);
            }
        }

        /// <summary>
        /// Altera o valor mensal de aporte do cliente.
        /// </summary>
        /// <param name="clienteId">ID do cliente.</param>
        /// <param name="request">Novo valor de aporte.</param>
        /// <returns>Confirmação da alteração de valor.</returns>
        [HttpPut("{clienteId}/valor-mensal")]
        [ProducesResponseType(typeof(AlterarValorMensalResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErroResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErroResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AlterarValorMensal(long clienteId, [FromBody] AlterarValorMensalRequestDto request)
        {
            try
            {
                var response = await _clienteService.AlterarValorMensalAsync(clienteId, request);
                return Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("|"))
            {
                var partes = ex.Message.Split('|');
                var erroResponse = new ErroResponseDto { Codigo = partes[0], Erro = partes[1] };

                if (partes[0] == "CLIENTE_NAO_ENCONTRADO")
                    return NotFound(erroResponse);

                return BadRequest(erroResponse);
            }
        }

        /// <summary>
        /// Consulta a carteira atualizada do cliente com cálculos de rentabilidade.
        /// </summary>
        /// <param name="clienteId">ID do cliente.</param>
        /// <returns>Posição em custódia, valores atuais e P/L.</returns>
        [HttpGet("{clienteId}/carteira")]
        [ProducesResponseType(typeof(ConsultaCarteiraResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErroResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConsultarCarteira(long clienteId)
        {
            try
            {
                var response = await _clienteService.ConsultarCarteiraAsync(clienteId);
                return Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("|"))
            {
                var partes = ex.Message.Split('|');
                return NotFound(new ErroResponseDto { Codigo = partes[0], Erro = partes[1] });
            }
        }

        /// <summary>
        /// Consulta o histórico detalhado de rentabilidade, aportes e evolução da carteira do cliente.
        /// </summary>
        /// <param name="clienteId">ID do cliente.</param>
        /// <returns>Resumo atual, histórico de aportes e linha do tempo da carteira.</returns>
        [HttpGet("{clienteId}/rentabilidade")]
        [ProducesResponseType(typeof(RentabilidadeDetalhadaResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErroResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConsultarRentabilidadeDetalhada(long clienteId)
        {
            try
            {
                var response = await _clienteService.ConsultarRentabilidadeDetalhadaAsync(clienteId);
                return Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("|"))
            {
                var partes = ex.Message.Split('|');
                return NotFound(new ErroResponseDto { Codigo = partes[0], Erro = partes[1] });
            }
        }

        [HttpGet("cpf/{cpf}")]
        public async Task<IActionResult> ObterPorCpf(string cpf)
        {
            try
            {
                var response = await _clienteService.ObterPorCpfAsync(cpf);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}