using System;
using Domain.Enums;

namespace Domain.Entities
{
    public class Rebalanceamento
    {
        public long Id { get; set; }
        public long ClienteId { get; set; }
        public TipoRebalanceamento Tipo { get; set; }
        public string TickerVendido { get; set; }
        public string TickerComprado { get; set; }
        public decimal ValorVenda { get; set; }
        public DateTime DataRebalanceamento { get; set; }

        public virtual Cliente Cliente { get; set; }
    }
}