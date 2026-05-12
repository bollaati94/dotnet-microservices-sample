using System.Text;
using System.Text.Json;
using Common.Models;
using RabbitMQ.Client;

namespace PublisherService;

public interface IGatePublisher
{
    Task PublishEventAsync(GateEvent gateEvent);
}

public class GatePublisher : IGatePublisher
{
    private readonly ILogger<GatePublisher> _logger;
    private readonly ConnectionFactory _factory;

    public GatePublisher(ILogger<GatePublisher> logger)
    {
        _logger = logger;
        _factory = new ConnectionFactory { HostName = "rabbitmq" };
    }

    public async Task PublishEventAsync(GateEvent gateEvent)
    {
        using var connection = await _factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        
        await channel.QueueDeclareAsync("gate_events_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(gateEvent));
        var properties = new BasicProperties { Persistent = true };

        await channel.BasicPublishAsync(exchange: "", routingKey: "gate_events_queue", mandatory: false, basicProperties: properties, body: body);
        _logger.LogInformation("Published event: {PersonId} {Type} at {Gate}", gateEvent.PersonId, gateEvent.EntryType, gateEvent.GateId);
    }
}

public class GateSimulationJob
{
    private readonly IGatePublisher _publisher;
    private readonly ILogger<GateSimulationJob> _logger;

    public GateSimulationJob(IGatePublisher publisher, ILogger<GateSimulationJob> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task RunSimulation()
    {
        var random = new Random();
        var personTypes = Enum.GetValues<PersonType>();
        var entryTypes = Enum.GetValues<EntryType>();
        var gates = new[] { "North-Gate", "South-Gate", "Main-Entrance", "Staff-Exit" };

        var gateEvent = new GateEvent(
            Guid.NewGuid(),
            gates[random.Next(gates.Length)],
            $"USER_{random.Next(1000, 9999)}",
            personTypes[random.Next(personTypes.Length)],
            entryTypes[random.Next(entryTypes.Length)],
            DateTime.UtcNow
        );

        await _publisher.PublishEventAsync(gateEvent);
    }
}
