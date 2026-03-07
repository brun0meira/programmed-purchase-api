using System;
using Domain.Enums;

namespace Domain.Entities
{
    public class EventoIR
    {
        public long Id { get; set; }
        public long ClienteId { get; set; }
        public TipoEventoIR Tipo { get; set; }
        public decimal ValorBase { get; set; }
        public decimal ValorIR { get; set; }
        public bool PublicadoKafka { get; set; }
        public DateTime DataEvento { get; set; }

        public virtual Cliente Cliente { get; set; }
    }
}