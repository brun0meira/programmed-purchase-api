using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.ExternalServices;

namespace Infrastructure.Data.ExternalServices
{
    public class CotacaoB3Service : ICotacaoB3Service
    {
        public async Task<Dictionary<string, decimal>> ObterCotacoesFechamentoAsync(DateTime dataReferencia, List<string> tickers)
        {
            // cotacoes na raiz da WebAPI
            var pastaCotacoes = Path.Combine(AppContext.BaseDirectory, "cotacoes");

            if (!Directory.Exists(pastaCotacoes))
                throw new DirectoryNotFoundException($"A pasta {pastaCotacoes} não foi encontrada.");

            // Pega todos os arquivos e ordena do mais recente pro mais antigo (alfabeticamente)
            var arquivos = Directory.GetFiles(pastaCotacoes, "COTAHIST_D*.TXT")
                                    .OrderByDescending(f => f)
                                    .ToList();

            string arquivoAlvo = null;

            // Encontra o arquivo mais recente que seja <= à data de referência do Motor
            foreach (var arquivo in arquivos)
            {
                var nomeArquivo = Path.GetFileNameWithoutExtension(arquivo);
                var dataString = nomeArquivo.Substring(10, 8);

                if (DateTime.TryParseExact(dataString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dataArquivo))
                {
                    if (dataArquivo.Date <= dataReferencia.Date)
                    {
                        arquivoAlvo = arquivo;
                        break;
                    }
                }
            }

            if (arquivoAlvo == null)
                throw new InvalidOperationException("COTACAO_NAO_ENCONTRADA|Arquivo COTAHIST nao encontrado para a data.");

            return await ProcessarArquivoAsync(arquivoAlvo, tickers);
        }

        private async Task<Dictionary<string, decimal>> ProcessarArquivoAsync(string caminhoArquivo, List<string> tickers)
        {
            var resultado = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            // Habilita o ISO-8859-1 exigido pela B3
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding("ISO-8859-1");

            using var stream = new StreamReader(caminhoArquivo, encoding);
            string linha;

            while ((linha = await stream.ReadLineAsync()) != null)
            {
                if (linha.Length < 245) continue;

                var tipoRegistro = linha.Substring(0, 2);
                if (tipoRegistro != "01") continue;

                var tickerLinha = linha.Substring(12, 12).Trim();

                // Só processa se a linha for de um ticker Top Five
                if (tickers.Contains(tickerLinha, StringComparer.OrdinalIgnoreCase))
                {
                    var tipoMercado = int.Parse(linha.Substring(24, 3).Trim());
                    var codigoBdi = linha.Substring(10, 2).Trim();

                    // Filtra apenas Mercado a Vista (010) e Lote Padrão (02)
                    if (tipoMercado == 10 && codigoBdi == "02")
                    {
                        var precoFechamentoBruto = linha.Substring(108, 13);
                        if (long.TryParse(precoFechamentoBruto.Trim(), out var valor))
                        {
                            var precoReal = valor / 100m; // Divide por 100

                            // Garante que só pegaremos uma cotação por ativo
                            if (!resultado.ContainsKey(tickerLinha))
                            {
                                resultado.Add(tickerLinha, precoReal);
                            }
                        }
                    }
                }

                if (resultado.Count == tickers.Count) break;
            }

            return resultado;
        }
    }
}