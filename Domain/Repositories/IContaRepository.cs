using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repositories
{
    public interface IContaRepository
    {
        Task<ContaGrafica> ObterContaMasterComCustodiaAsync();
        Task AdicionarAsync(ContaGrafica conta);
        Task SalvarAlteracoesAsync();
    }
}