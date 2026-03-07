using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly AppDbContext _context;

        public ClienteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CpfExisteAsync(string cpf)
        {
            return await _context.Clientes.AnyAsync(c => c.Cpf == cpf);
        }

        public async Task<Cliente> ObterPorIdAsync(long id)
        {
            return await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AdicionarAsync(Cliente cliente)
        {
            await _context.Clientes.AddAsync(cliente);
        }

        public async Task AtualizarAsync(Cliente cliente)
        {
            _context.Clientes.Update(cliente);
            await Task.CompletedTask;
        }

        public async Task SalvarAlteracoesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Cliente> ObterClienteComCustodiaAsync(long id)
        {
            return await _context.Clientes
                .Include(c => c.ContasGraficas)
                    .ThenInclude(cg => cg.Custodias)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Cliente> ObterClienteComHistoricoAsync(long id)
        {
            return await _context.Clientes
                .Include(c => c.ContasGraficas)
                    .ThenInclude(cg => cg.Custodias)
                        .ThenInclude(c => c.Distribuicoes)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Cliente>> ObterClientesAtivosComCustodiaAsync()
        {
            return await _context.Clientes
                .Include(c => c.ContasGraficas)
                    .ThenInclude(cg => cg.Custodias)
                .Where(c => c.Ativo)
                .ToListAsync();
        }
    }
}