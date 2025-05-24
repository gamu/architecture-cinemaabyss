using Microsoft.AspNetCore.Mvc;
using EventsService.Services;

namespace EventsService.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IKafkaProducerService kafkaProducer, ILogger<EventsController> logger)
    {
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new { status = true });
    }

    [HttpPost("movie")]
    public async Task<IActionResult> SendMovieEvent()
    {
        try
        {
            await _kafkaProducer.SendMessageAsync("movie-events", "movie message");
            return CreatedAtAction(nameof(SendMovieEvent), new { status = "success" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending movie event");
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }

    [HttpPost("user")]
    public async Task<IActionResult> SendUserEvent()
    {
        try
        {
            await _kafkaProducer.SendMessageAsync("user-events", "user message");
            return CreatedAtAction(nameof(SendUserEvent), new { status = "success" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending user event");
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }

    [HttpPost("payment")]
    public async Task<IActionResult> SendPaymentEvent()
    {
        try
        {
            await _kafkaProducer.SendMessageAsync("payment-events", "payment message");
            return CreatedAtAction(nameof(SendPaymentEvent), new { status = "success" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment event");
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }
} 