using System.Threading.Tasks;
using Domain.Dto.Cliente;

namespace Domain.Business
{
    public interface IClienteService
    {
        Task<AdesaoResponseDto> AderirProdutoAsync(AdesaoRequestDto request);
        Task<SaidaResponseDto> SairProdutoAsync(long clienteId);
        Task<AlterarValorMensalResponseDto> AlterarValorMensalAsync(long clienteId, AlterarValorMensalRequestDto request);
        Task<ConsultaCarteiraResponseDto> ConsultarCarteiraAsync(long clienteId);
        Task<RentabilidadeDetalhadaResponseDto> ConsultarRentabilidadeDetalhadaAsync(long clienteId);
        Task<ClienteResumoDto> ObterPorCpfAsync(string cpf);
    }
}