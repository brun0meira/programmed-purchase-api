using System;
using System.Threading.Tasks;

namespace Domain.Business
{
    public interface IRebalanceamentoService
    {
        Task<string> ExecutarRebalanceamentoAsync(DateTime dataReferencia);
    }
}