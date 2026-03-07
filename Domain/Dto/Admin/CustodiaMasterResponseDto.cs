using System.Collections.Generic;

namespace Domain.Dto.Admin
{
    public class CustodiaMasterResponseDto
    {
        public ContaMasterDto ContaMaster { get; set; }
        public List<ItemCustodiaMasterDto> Custodia { get; set; } = new List<ItemCustodiaMasterDto>();
        public decimal ValorTotalResiduo { get; set; }
    }

    public class ContaMasterDto
    {
        public long Id { get; set; }
        public string NumeroConta { get; set; }
        public string Tipo { get; set; }
    }

    public class ItemCustodiaMasterDto
    {
        public string Ticker { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoMedio { get; set; }
        public decimal ValorAtual { get; set; }
        public string Origem { get; set; }
    }
}