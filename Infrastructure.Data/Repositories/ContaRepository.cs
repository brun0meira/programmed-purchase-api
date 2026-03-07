using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    public class ContaRepository : IContaRepository
    {
        private readonly AppDbContext _context;

        public ContaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ContaGrafica> ObterContaMasterComCustodiaAsync()
        {
            return await _context.ContasGraficas
                .Include(c => c.Custodias)
                .FirstOrDefaultAsync(c => c.Tipo == TipoConta.Master);
        }

        public async Task AdicionarAsync(ContaGrafica conta)
        {
            await _context.ContasGraficas.AddAsync(conta);
        }

        public async Task SalvarAlteracoesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}