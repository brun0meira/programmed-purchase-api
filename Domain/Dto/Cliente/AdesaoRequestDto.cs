using System.ComponentModel.DataAnnotations;

namespace Domain.Dto.Cliente
{
    public class AdesaoRequestDto
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "O CPF deve ter 11 caracteres.")]
        public string Cpf { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O valor mensal é obrigatório.")]
        [Range(100.00, double.MaxValue, ErrorMessage = "O valor mensal minimo e de R$ 100,00.")]
        public decimal ValorMensal { get; set; }
    }
}