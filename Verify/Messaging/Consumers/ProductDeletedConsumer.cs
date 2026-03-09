using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VERIFY.Messaging.Events;
using VERIFY.Model;

namespace VERIFY.Messaging.Consumers;

public sealed class ProductDeletedConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IRabbitMqConnection _rabbitConnection;
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductDeletedConsumer> _logger;

    private IModel? _channel;

    public ProductDeletedConsumer(
        IRabbitMqConnection rabbitConnection,
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ProductDeletedConsumer> logger)
    {
        _rabbitConnection = rabbitConnection;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _rabbitConnection.Connection.CreateModel();

        _channel.ExchangeDeclare(exchange: _options.ExchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false);

        const string queueName = "product.deleted";
        const string routingKey = "product.deleted";

        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: queueName, exchange: _options.ExchangeName, routingKey: routingKey);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageAsync;

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel == null) return;

        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            var message = JsonSerializer.Deserialize<ProductDeletedEvent>(json, JsonOptions);

            if (message == null || message.productId <= 0)
            {
                _logger.LogWarning("Invalid message on {RoutingKey}", args.RoutingKey);
                _channel.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MACUTIONDB>();

            var record = await db.VERIFY_PRODUCTS.Where(x=>x.ProductId==message.productId).FirstOrDefaultAsync();
            if (record == null)
            {
                _channel.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }
            if (record.SellerId != message.deletedByUserId)
            {
                _channel.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            db.VERIFY_PRODUCTS.Remove(record);
            await db.SaveChangesAsync();

            _logger.LogInformation("Removed verification for deleted product {ProductId}", message.productId);
            _channel.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed processing {RoutingKey}", args.RoutingKey);
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
        catch
        {
        }

        base.Dispose();
    }
}

