using System;

namespace Domain.Dto.Cliente
{
    public class SaidaResponseDto
    {
        public long ClienteId { get; set; }
        public string Nome { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataSaida { get; set; }
        public string Mensagem { get; set; }
    }
}