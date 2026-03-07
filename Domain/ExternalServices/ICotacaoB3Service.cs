using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.ExternalServices
{
    public interface ICotacaoB3Service
    {
        Task<Dictionary<string, decimal>> ObterCotacoesFechamentoAsync(DateTime dataReferencia, List<string> tickers);
    }
}