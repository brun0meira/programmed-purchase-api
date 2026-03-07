using System;

namespace Domain.Dto.Cliente
{
    public class AlterarValorMensalResponseDto
    {
        public long ClienteId { get; set; }
        public decimal ValorMensalAnterior { get; set; }
        public decimal ValorMensalNovo { get; set; }
        public DateTime DataAlteracao { get; set; }
        public string Mensagem { get; set; }
    }
}