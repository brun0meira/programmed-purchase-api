using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repositories
{
    public interface IClienteRepository
    {
        Task<bool> CpfExisteAsync(string cpf);
        Task<Cliente> ObterPorIdAsync(long id);
        Task AdicionarAsync(Cliente cliente);
        Task AtualizarAsync(Cliente cliente);
        Task SalvarAlteracoesAsync();
        Task<Cliente> ObterClienteComCustodiaAsync(long id);
        Task<Cliente> ObterClienteComHistoricoAsync(long id);
        Task<List<Cliente>> ObterClientesAtivosComCustodiaAsync();
    }
}