using System.Threading.Tasks;
using Domain.Dto.Motor;

namespace Domain.Business
{
    public interface IMotorCompraService
    {
        Task<ExecutarCompraResponseDto> ExecutarCompraProgramadaAsync(ExecutarCompraRequestDto request);
    }
}