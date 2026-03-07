using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    public class CestaRepository : ICestaRepository
    {
        private readonly AppDbContext _context;

        public CestaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CestaRecomendacao> ObterCestaAtualAsync()
        {
            return await _context.CestasRecomendacao
                .Include(c => c.Itens)
                .Where(c => c.Ativa)
                .FirstOrDefaultAsync();
        }

        public async Task<List<CestaRecomendacao>> ObterHistoricoAsync()
        {
            return await _context.CestasRecomendacao
                .Include(c => c.Itens)
                .OrderByDescending(c => c.DataCriacao)
                .ToListAsync();
        }

        public async Task AdicionarAsync(CestaRecomendacao cesta)
        {
            await _context.CestasRecomendacao.AddAsync(cesta);
        }

        public async Task AtualizarAsync(CestaRecomendacao cesta)
        {
            _context.CestasRecomendacao.Update(cesta);
            await Task.CompletedTask;
        }

        public async Task SalvarAlteracoesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}