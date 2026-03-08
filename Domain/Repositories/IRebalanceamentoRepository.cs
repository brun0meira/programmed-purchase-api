using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repositories
{
    public interface IRebalanceamentoRepository
    {
        Task AdicionarAsync(Rebalanceamento rebalanceamento);
    }
}