using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;

namespace Infrastructure.Data.Repositories
{
    public class RebalanceamentoRepository : IRebalanceamentoRepository
    {
        private readonly AppDbContext _context;

        public RebalanceamentoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AdicionarAsync(Rebalanceamento rebalanceamento)
        {
            await _context.Rebalanceamentos.AddAsync(rebalanceamento);
        }
    }
}