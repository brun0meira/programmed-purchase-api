using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class CestaRecomendacao
    {
        public long Id { get; set; }
        public string Nome { get; set; }
        public bool Ativa { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataDesativacao { get; set; }

        public virtual ICollection<ItemCesta> Itens { get; set; } = new List<ItemCesta>();
    }
}