using System;
using System.Collections.Generic;

namespace Domain.Dto.Cliente
{
    public class RentabilidadeDetalhadaResponseDto
    {
        public long ClienteId { get; set; }
        public string Nome { get; set; }
        public DateTime DataConsulta { get; set; }
        public ResumoCarteiraDto Rentabilidade { get; set; }
        public List<HistoricoAporteDto> HistoricoAportes { get; set; } = new List<HistoricoAporteDto>();
        public List<EvolucaoCarteiraDto> EvolucaoCarteira { get; set; } = new List<EvolucaoCarteiraDto>();
    }

    public class HistoricoAporteDto
    {
        public string Data { get; set; }
        public decimal Valor { get; set; }
        public string Parcela { get; set; }
    }

    public class EvolucaoCarteiraDto
    {
        public string Data { get; set; }
        public decimal ValorCarteira { get; set; }
        public decimal ValorInvestido { get; set; }
        public decimal Rentabilidade { get; set; }
    }
}