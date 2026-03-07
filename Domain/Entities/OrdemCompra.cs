using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Domain.Entities
{
    public class OrdemCompra
    {
        public long Id { get; set; }
        public long ContaMasterId { get; set; }
        public string Ticker { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public TipoMercado TipoMercado { get; set; }
        public DateTime DataExecucao { get; set; }

        public virtual ContaGrafica ContaMaster { get; set; }
        public virtual ICollection<Distribuicao> Distribuicoes { get; set; } = new List<Distribuicao>();
    }
}