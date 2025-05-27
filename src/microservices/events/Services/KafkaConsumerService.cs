using Confluent.Kafka;

namespace EventsService.Services;

public class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly string _brokers;
    private readonly string[] _topics = { "movie-events", "user-events", "payment-events" };
    private const int MaxRetries = 10;
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);

    public KafkaConsumerService(string brokers, ILogger<KafkaConsumerService>? logger = null)
    {
        _brokers = brokers;
        _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<KafkaConsumerService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Задержка для инициализации как в оригинальном Go приложении
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        var consumer = InitializeConsumerWithRetry();
        
        try
        {
            consumer.Subscribe(_topics);
            _logger.LogInformation("Kafka consumer subscribed to topics: {Topics}", string.Join(", ", _topics));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    
                    if (consumeResult?.Message != null)
                    {
                        _logger.LogInformation("[Topic: {Topic}] Message: {Message}", 
                            consumeResult.Topic, consumeResult.Message.Value);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error occurred during message consumption");
                }
                catch (OperationCanceledException)
                {
                    // Ожидаемое исключение при остановке сервиса
                    break;
                }
            }
        }
        finally
        {
            consumer.Close();
            consumer.Dispose();
        }
    }

    private IConsumer<Ignore, string> InitializeConsumerWithRetry()
    {
        for (int i = 0; i < MaxRetries; i++)
        {
            try
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = _brokers,
                    GroupId = "events-service-consumer-group",
                    AutoOffsetReset = AutoOffsetReset.Latest,
                    EnableAutoCommit = true
                };

                var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
                _logger.LogInformation("Kafka consumer initialized successfully");
                return consumer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to init consumer (attempt {Attempt}/{MaxRetries})", 
                    i + 1, MaxRetries);
                
                if (i < MaxRetries - 1)
                {
                    Thread.Sleep(RetryInterval);
                }
            }
        }

        throw new InvalidOperationException($"Failed to init consumer after {MaxRetries} retries");
    }
} 