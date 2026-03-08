using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Domain.ExternalServices;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Queue
{
    public class KafkaProducerService : IKafkaProducerService
    {
        private readonly string _bootstrapServers;
        private readonly string _topicName = "topico-eventos-ir";

        public KafkaProducerService(IConfiguration configuration)
        {
            _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        }

        public async Task PublicarEventoIRAsync(long clienteId, string tipoEvento, decimal valorOperacao, decimal valorIR, string dataReferencia)
        {
            var config = new ProducerConfig { BootstrapServers = _bootstrapServers };

            using var producer = new ProducerBuilder<Null, string>(config).Build();

            // payload que a Receita Federal (simulada) receberia
            var evento = new
            {
                ClienteId = clienteId,
                TipoEvento = tipoEvento,
                ValorOperacao = valorOperacao,
                ValorIR = valorIR,
                DataReferencia = dataReferencia,
                DataPublicacao = DateTime.UtcNow
            };

            var mensagemJson = JsonSerializer.Serialize(evento);

            try
            {
                var result = await producer.ProduceAsync(_topicName, new Message<Null, string> { Value = mensagemJson });
                Console.WriteLine($"[KAFKA PRODUCER] Evento de IR do Cliente {clienteId} publicado no tópico {result.Topic} | Partição: {result.Partition} | Offset: {result.Offset}");
            }
            catch (ProduceException<Null, string> e)
            {
                Console.WriteLine($"[KAFKA PRODUCER ERRO] Falha ao enviar evento: {e.Error.Reason}");
                throw new InvalidOperationException("KAFKA_INDISPONIVEL|Erro ao publicar no topico Kafka.");
            }
        }
    }
}