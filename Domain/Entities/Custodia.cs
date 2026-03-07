using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Custodia
    {
        public long Id { get; set; }
        public long ContaGraficaId { get; set; }
        public string Ticker { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoMedio { get; set; }
        public DateTime DataUltimaAtualizacao { get; set; }

        public virtual ContaGrafica ContaGrafica { get; set; }
        public virtual ICollection<Distribuicao> Distribuicoes { get; set; } = new List<Distribuicao>();
    }
}