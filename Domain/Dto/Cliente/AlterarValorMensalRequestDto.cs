using System.ComponentModel.DataAnnotations;

namespace Domain.Dto.Cliente
{
    public class AlterarValorMensalRequestDto
    {
        [Required(ErrorMessage = "O novo valor mensal é obrigatório.")]
        [Range(100.00, double.MaxValue, ErrorMessage = "O valor mensal minimo e de R$ 100,00.")]
        public decimal NovoValorMensal { get; set; }
    }
}