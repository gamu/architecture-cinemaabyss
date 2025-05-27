namespace EventsService.Services;

public interface IKafkaProducerService
{
    Task SendMessageAsync(string topic, string message);
} 