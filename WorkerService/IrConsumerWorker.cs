using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WorkerService
{
    public class IrConsumerWorker : BackgroundService
    {
        private readonly ILogger<IrConsumerWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly string _bootstrapServers;
        private readonly string _topicName = "topico-eventos-ir";
        private readonly string _groupId = "grupo-receita-federal";

        public IrConsumerWorker(ILogger<IrConsumerWorker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[KAFKA CONSUMER] Iniciando o consumo de eventos de IR...");

            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(_topicName);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);
                        var mensagemJson = consumeResult.Message.Value;

                        _logger.LogInformation($"\n[RECEITA FEDERAL] Processando IR... Offset: {consumeResult.Offset}");

                        // 1. Desserializar a mensagem
                        using var doc = JsonDocument.Parse(mensagemJson);
                        var clienteId = doc.RootElement.GetProperty("ClienteId").GetInt64();
                        var valorIr = doc.RootElement.GetProperty("ValorIR").GetDecimal();
                        var dataRef = doc.RootElement.GetProperty("DataReferencia").GetString();

                        // 2. Abrir um escopo para usar o Entity Framework no Worker
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                            // 3. Buscar o evento pendente no banco e atualizar
                            var eventoPendente = await dbContext.EventosIR
                                .FirstOrDefaultAsync(e => e.ClienteId == clienteId
                                                       && e.ValorIR == valorIr
                                                       && e.PublicadoKafka == false, stoppingToken);

                            if (eventoPendente != null)
                            {
                                eventoPendente.PublicadoKafka = true;
                                await dbContext.SaveChangesAsync(stoppingToken);
                                _logger.LogInformation($"[SUCESSO] Evento de IR do Cliente {clienteId} (R$ {valorIr}) confirmado no banco de dados!");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Em produção, aqui enviaríamos para uma Dead Letter Queue (DLQ)
                        _logger.LogError($"Erro ao processar mensagem: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                consumer.Close();
            }
        }
    }
}