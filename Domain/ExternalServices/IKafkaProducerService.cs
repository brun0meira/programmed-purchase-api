using System.Threading.Tasks;

namespace Domain.ExternalServices
{
    public interface IKafkaProducerService
    {
        Task PublicarEventoIRAsync(long clienteId, string tipoEvento, decimal valorOperacao, decimal valorIR, string dataReferencia);
    }
}