using System;

namespace Domain.Entities
{
    public class Distribuicao
    {
        public long Id { get; set; }
        public long OrdemCompraId { get; set; }
        public long CustodiaFilhoteId { get; set; }
        public string Ticker { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public DateTime DataDistribuicao { get; set; }

        public virtual OrdemCompra OrdemCompra { get; set; }
        public virtual Custodia CustodiaFilhote { get; set; }
    }
}