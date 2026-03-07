using Domain.Business;
using Domain.Dto.Admin;
using Domain.Dto.Cliente;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ProjectWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AdminController : ControllerBase
    {
        private readonly ICestaService _cestaService;
        private readonly IContaMasterService _contaMasterService;

        public AdminController(ICestaService cestaService, IContaMasterService contaMasterService)
        {
            _cestaService = cestaService;
            _contaMasterService = contaMasterService;
        }

        /// <summary>
        /// Cadastra ou altera a cesta Top Five de recomendação.
        /// </summary>
        [HttpPost("cesta")]
        [ProducesResponseType(typeof(CestaResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErroResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CadastrarCesta([FromBody] CriarCestaRequestDto request)
        {
            try
            {
                var response = await _cestaService.CadastrarCestaAsync(request);
                return Created("/api/admin/cesta/atual", response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("|"))
            {
                var partes = ex.Message.Split('|');
                return BadRequest(new ErroResponseDto { Codigo = partes[0], Erro = partes[1] });
            }
        }

        /// <summary>
        /// Consulta a composição atual da cesta de recomendação.
        /// </summary>
        [HttpGet("cesta/atual")]
        [ProducesResponseType(typeof(CestaResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErroResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterCestaAtual()
        {
            try
            {
                var response = await _cestaService.ConsultarCestaAtualAsync();
                return Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("|"))
            {
                var partes = ex.Message.Split('|');
                return NotFound(new ErroResponseDto { Codigo = partes[0], Erro = partes[1] });
            }
        }

        /// <summary>
        /// Retorna o histórico de todas as cestas já cadastradas.
        /// </summary>
        [HttpGet("cesta/historico")]
        [ProducesResponseType(typeof(HistoricoCestasResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObterHistoricoCestas()
        {
            var response = await _cestaService.ObterHistoricoCestasAsync();
            return Ok(response);
        }

        /// <summary>
        /// Consulta a custódia residual consolidada na Conta Master da corretora.
        /// </summary>
        [HttpGet("conta-master/custodia")]
        [ProducesResponseType(typeof(CustodiaMasterResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObterCustodiaMaster()
        {
            var response = await _contaMasterService.ConsultarCustodiaMasterAsync();
            return Ok(response);
        }

        /// <summary>
        /// Popula o banco de dados com clientes e contas filhotes para facilitar os testes da aplicação.
        /// </summary>
        [HttpPost("seed-dados-teste")]
        public async Task<IActionResult> PopularBancoParaTestes([FromServices] Infrastructure.Data.AppDbContext context)
        {
            if (context.Clientes.Any())
                return BadRequest("O banco já possui dados.");

            var clientes = new List<Cliente>
            {
                new Cliente { Nome = "João Silva", Cpf = "11122233344", Email = "joao@teste.com", Ativo = true, ValorMensal = 3000m },
                new Cliente { Nome = "Maria Souza", Cpf = "55566677788", Email = "maria@teste.com", Ativo = true, ValorMensal = 6000m },
                new Cliente { Nome = "Carlos Eduardo", Cpf = "99900011122", Email = "carlos@teste.com", Ativo = true, ValorMensal = 1500m }
            };

            foreach (var c in clientes)
            {
                c.ContasGraficas.Add(new ContaGrafica
                {
                    NumeroConta = $"FIL-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                    Tipo = TipoConta.Filhote,
                    DataCriacao = DateTime.UtcNow
                });
            }

            await context.Clientes.AddRangeAsync(clientes);
            await context.SaveChangesAsync();

            return Ok(new { Mensagem = "Banco populado com 3 clientes de teste! Valores mensais: R$ 3000, R$ 6000 e R$ 1500." });
        }
    }
}