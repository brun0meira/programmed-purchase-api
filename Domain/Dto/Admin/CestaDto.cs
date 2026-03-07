using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain.Dto.Admin
{
    public class CriarCestaRequestDto
    {
        [Required(ErrorMessage = "O nome da cesta é obrigatório.")]
        public string Nome { get; set; }

        [Required]
        public List<ItemCestaRequestDto> Itens { get; set; } = new List<ItemCestaRequestDto>();
    }

    public class ItemCestaRequestDto
    {
        [Required]
        public string Ticker { get; set; }

        [Range(0.01, 100.00)]
        public decimal Percentual { get; set; }
    }

    public class CestaResponseDto
    {
        public long CestaId { get; set; }
        public string Nome { get; set; }
        public bool Ativa { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataDesativacao { get; set; }
        public List<ItemCestaResponseDto> Itens { get; set; } = new List<ItemCestaResponseDto>();

        // Propriedades para quando há substituição de cesta
        public CestaAnteriorDto CestaAnteriorDesativada { get; set; }
        public bool RebalanceamentoDisparado { get; set; }
        public List<string> AtivosRemovidos { get; set; }
        public List<string> AtivosAdicionados { get; set; }
        public string Mensagem { get; set; }
    }

    public class ItemCestaResponseDto
    {
        public string Ticker { get; set; }
        public decimal Percentual { get; set; }
        public decimal? CotacaoAtual { get; set; } 
    }

    public class CestaAnteriorDto
    {
        public long CestaId { get; set; }
        public string Nome { get; set; }
        public DateTime? DataDesativacao { get; set; }
    }

    public class HistoricoCestasResponseDto
    {
        public List<CestaResponseDto> Cestas { get; set; } = new List<CestaResponseDto>();
    }
}