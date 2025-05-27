using Confluent.Kafka;

namespace EventsService.Services;

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly string _brokers;
    private const int MaxRetries = 10;
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);

    public KafkaProducerService(string brokers, ILogger<KafkaProducerService>? logger = null)
    {
        _brokers = brokers;
        _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<KafkaProducerService>();
        _producer = InitializeProducerWithRetry();
    }

    private IProducer<Null, string> InitializeProducerWithRetry()
    {
        for (int i = 0; i < MaxRetries; i++)
        {
            try
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = _brokers,
                    EnableDeliveryReports = true,
                    DeliveryReportFields = "all"
                };

                var producer = new ProducerBuilder<Null, string>(config).Build();
                _logger.LogInformation("Kafka producer initialized successfully");
                return producer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to init producer (attempt {Attempt}/{MaxRetries})", 
                    i + 1, MaxRetries);
                
                if (i < MaxRetries - 1)
                {
                    Thread.Sleep(RetryInterval);
                }
            }
        }

        throw new InvalidOperationException($"Failed to init producer after {MaxRetries} retries");
    }

    public async Task SendMessageAsync(string topic, string message)
    {
        try
        {
            var kafkaMessage = new Message<Null, string>
            {
                Value = message
            };

            var result = await _producer.ProduceAsync(topic, kafkaMessage);
            _logger.LogInformation("Message sent to topic {Topic}: {Message}", topic, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to topic {Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
} 