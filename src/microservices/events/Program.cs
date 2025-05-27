using EventsService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Kafka
var kafkaBrokers = Environment.GetEnvironmentVariable("KAFKA_BROKERS") ?? "localhost:9092";

// Register Kafka services
builder.Services.AddSingleton<IKafkaProducerService>(serviceProvider => 
{
    var logger = serviceProvider.GetRequiredService<ILogger<KafkaProducerService>>();
    return new KafkaProducerService(kafkaBrokers, logger);
});

builder.Services.AddHostedService<KafkaConsumerService>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<KafkaConsumerService>>();
    return new KafkaConsumerService(kafkaBrokers, logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8082";
app.Run($"http://*:{port}"); 