using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Cliente
    {
        public long Id { get; set; }
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string Email { get; set; }
        public decimal ValorMensal { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime DataAdesao { get; set; }
        public virtual ICollection<ContaGrafica> ContasGraficas { get; set; } = new List<ContaGrafica>();
        public virtual ICollection<EventoIR> EventosIR { get; set; } = new List<EventoIR>();
        public virtual ICollection<Rebalanceamento> Rebalanceamentos { get; set; } = new List<Rebalanceamento>();
    }
}