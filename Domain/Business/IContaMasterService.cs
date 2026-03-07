using System.Threading.Tasks;
using Domain.Dto.Admin;

namespace Domain.Business
{
    public interface IContaMasterService
    {
        Task<CustodiaMasterResponseDto> ConsultarCustodiaMasterAsync();
    }
}