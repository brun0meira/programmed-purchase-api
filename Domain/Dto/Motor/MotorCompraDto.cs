using System;
using System.Collections.Generic;

namespace Domain.Dto.Motor
{
    public class ExecutarCompraRequestDto
    {
        public string DataReferencia { get; set; }
    }

    public class ExecutarCompraResponseDto
    {
        public DateTime DataExecucao { get; set; }
        public int TotalClientes { get; set; }
        public decimal TotalConsolidado { get; set; }
        public List<OrdemCompraResponseDto> OrdensCompra { get; set; } = new List<OrdemCompraResponseDto>();
        public List<DistribuicaoClienteDto> Distribuicoes { get; set; } = new List<DistribuicaoClienteDto>();
        public List<ResiduoMasterDto> ResiduosCustMaster { get; set; } = new List<ResiduoMasterDto>();
        public int EventosIRPublicados { get; set; }
        public string Mensagem { get; set; }
    }

    public class OrdemCompraResponseDto
    {
        public string Ticker { get; set; }
        public int QuantidadeTotal { get; set; }
        public List<OrdemDetalheDto> Detalhes { get; set; } = new List<OrdemDetalheDto>();
        public decimal PrecoUnitario { get; set; }
        public decimal ValorTotal { get; set; }
    }

    public class OrdemDetalheDto
    {
        public string Tipo { get; set; } // LOTE_PADRAO ou FRACIONARIO
        public string Ticker { get; set; } // Ex: PETR4 ou PETR4F
        public int Quantidade { get; set; }
    }

    public class DistribuicaoClienteDto
    {
        public long ClienteId { get; set; }
        public string Nome { get; set; }
        public decimal ValorAporte { get; set; }
        public List<AtivoDistribuidoDto> Ativos { get; set; } = new List<AtivoDistribuidoDto>();
    }

    public class AtivoDistribuidoDto
    {
        public string Ticker { get; set; }
        public int Quantidade { get; set; }
    }

    public class ResiduoMasterDto
    {
        public string Ticker { get; set; }
        public int Quantidade { get; set; }
    }
}