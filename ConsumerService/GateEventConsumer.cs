using System.Text;
using System.Text.Json;
using Common.Models;
using Nest;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ConsumerService;

public class GateEventConsumer : BackgroundService
{
    private readonly ILogger<GateEventConsumer> _logger;
    private readonly IElasticClient _elasticClient;
    private IConnection? _rabbitConnection;
    private IChannel? _channel;
    private const string QueueName = "gate_events_queue";
    private const string IndexName = "gate-events";

    public GateEventConsumer(ILogger<GateEventConsumer> logger, IElasticClient elasticClient)
    {
        _logger = logger;
        _elasticClient = elasticClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        _rabbitConnection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _rabbitConnection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
        await _channel.BasicQosAsync(0, 10, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try 
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var gateEvent = JsonSerializer.Deserialize<GateEvent>(message);

                if (gateEvent != null)
                {
                    _logger.LogInformation("Processing event: {PersonId} {Type} at {Gate}", gateEvent.PersonId, gateEvent.EntryType, gateEvent.GateId);
                    
                    // Explicitly provide the index name to avoid the "Index name is null" exception
                    var response = await _elasticClient.IndexAsync(gateEvent, i => i.Index(IndexName));

                    if (response.IsValid)
                    {
                        await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    }
                    else
                    {
                        _logger.LogError("Elasticsearch indexing failed: {Error}", response.DebugInformation);
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
                    }
                }
                else
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync();
        if (_rabbitConnection != null) await _rabbitConnection.CloseAsync();
        await base.StopAsync(cancellationToken);
    }
}
