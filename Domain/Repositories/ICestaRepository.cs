using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repositories
{
    public interface ICestaRepository
    {
        Task<CestaRecomendacao> ObterCestaAtualAsync();
        Task<List<CestaRecomendacao>> ObterHistoricoAsync();
        Task AdicionarAsync(CestaRecomendacao cesta);
        Task AtualizarAsync(CestaRecomendacao cesta);
        Task SalvarAlteracoesAsync();
    }
}