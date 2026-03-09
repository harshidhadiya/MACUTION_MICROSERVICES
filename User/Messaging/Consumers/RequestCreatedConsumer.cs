using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using USER.Messaging.Events;

namespace USER.Messaging.Consumers;

/// <summary>
/// Consumes request.created events published by ADMIN when a new admin request is created.
/// User service uses this for audit/logging and to keep in sync (e.g. dashboard can show "request submitted").
/// </summary>
public sealed class RequestCreatedConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IRabbitMqConnection _rabbitConnection;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RequestCreatedConsumer> _logger;
    private IModel? _channel;

    public RequestCreatedConsumer(
        IRabbitMqConnection rabbitConnection,
        IOptions<RabbitMqOptions> options,
        ILogger<RequestCreatedConsumer> logger)
    {
        _rabbitConnection = rabbitConnection;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _rabbitConnection.Connection.CreateModel();
        _channel.ExchangeDeclare(exchange: _options.ExchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false);

        const string queueName = "user.request-created";
        const string routingKey = "request.created";

        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: queueName, exchange: _options.ExchangeName, routingKey: routingKey);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageAsync;
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("RequestCreatedConsumer started, listening for {RoutingKey}", routingKey);
        return Task.CompletedTask;
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel == null) return;

        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            var message = JsonSerializer.Deserialize<RequestCreatedEvent>(json, JsonOptions);

            if (message == null || message.RequestId <= 0)
            {
                _logger.LogWarning("Invalid request.created message received");
                _channel.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            _logger.LogInformation(
                "Request created: RequestId={RequestId}, UserId={UserId} (consumed by User service)",
                message.RequestId,
                message.RequestUserId);

            _channel.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process request.created");
            _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
        }
        catch { /* ignore */ }
        base.Dispose();
    }
}
