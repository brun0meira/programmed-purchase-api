using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Domain.Entities
{
    public class ContaGrafica
    {
        public long Id { get; set; }
        public long? ClienteId { get; set; } // Nullable porque a Conta Master não tem um Cliente específico
        public string NumeroConta { get; set; }
        public TipoConta Tipo { get; set; }
        public DateTime DataCriacao { get; set; }

        public virtual Cliente Cliente { get; set; }
        public virtual ICollection<Custodia> Custodias { get; set; } = new List<Custodia>();
        public virtual ICollection<OrdemCompra> OrdensCompra { get; set; } = new List<OrdemCompra>();
    }
}