using System;

namespace Domain.Dto.Cliente
{
    public class AdesaoResponseDto
    {
        public long ClienteId { get; set; }
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string Email { get; set; }
        public decimal ValorMensal { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataAdesao { get; set; }
        public ContaGraficaDto ContaGrafica { get; set; }
    }

    public class ContaGraficaDto
    {
        public long Id { get; set; }
        public string NumeroConta { get; set; }
        public string Tipo { get; set; }
        public DateTime DataCriacao { get; set; }
    }

    public class ErroResponseDto
    {
        public string Erro { get; set; }
        public string Codigo { get; set; }
    }
}