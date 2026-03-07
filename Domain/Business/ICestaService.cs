using System.Threading.Tasks;
using Domain.Dto.Admin;

namespace Domain.Business
{
    public interface ICestaService
    {
        Task<CestaResponseDto> CadastrarCestaAsync(CriarCestaRequestDto request);
        Task<CestaResponseDto> ConsultarCestaAtualAsync();
        Task<HistoricoCestasResponseDto> ObterHistoricoCestasAsync();
    }
}