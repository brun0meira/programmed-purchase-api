using System;
using System.Collections.Generic;

namespace Domain.Dto.Cliente
{
    public class ConsultaCarteiraResponseDto
    {
        public long ClienteId { get; set; }
        public string Nome { get; set; }
        public string ContaGrafica { get; set; }
        public DateTime DataConsulta { get; set; }
        public ResumoCarteiraDto Resumo { get; set; }
        public List<AtivoCarteiraDto> Ativos { get; set; } = new List<AtivoCarteiraDto>();
    }

    public class ResumoCarteiraDto
    {
        public decimal ValorTotalInvestido { get; set; }
        public decimal ValorAtualCarteira { get; set; }
        public decimal PlTotal { get; set; }
        public decimal RentabilidadePercentual { get; set; }
    }

    public class AtivoCarteiraDto
    {
        public string Ticker { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoMedio { get; set; }
        public decimal CotacaoAtual { get; set; }
        public decimal ValorAtual { get; set; }
        public decimal Pl { get; set; }
        public decimal PlPercentual { get; set; }
        public decimal ComposicaoCarteira { get; set; }
    }
}