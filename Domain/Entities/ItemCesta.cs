namespace Domain.Entities
{
    public class ItemCesta
    {
        public long Id { get; set; }
        public long CestaId { get; set; }
        public string Ticker { get; set; }
        public decimal Percentual { get; set; }

        public virtual CestaRecomendacao Cesta { get; set; }
    }
}