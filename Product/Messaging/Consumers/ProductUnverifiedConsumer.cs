using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PRODUCT.Messaging.Events;
using PRODUCT.Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PRODUCT.Messaging.Consumers;

public sealed class ProductUnverifiedConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IRabbitMqConnection _rabbitConnection;
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductUnverifiedConsumer> _logger;

    private IModel? _channel;

    public ProductUnverifiedConsumer(
        IRabbitMqConnection rabbitConnection,
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ProductUnverifiedConsumer> logger)
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

        const string queueName = "product.unverified";
        const string routingKey = "product.unverified";

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
            var message = JsonSerializer.Deserialize<ProductUnverifiedEvent>(json, JsonOptions);
             Console.WriteLine(message);
            if (message == null || message.productId <= 0)
            {
                _logger.LogWarning("Invalid message on {RoutingKey}", args.RoutingKey);
                _channel.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MACUTIONDB>();

            var product = await db.PRODUCTS.FirstOrDefaultAsync(p => p.Id == message.productId);
            if (product == null)
            {
                _logger.LogInformation("Product {ProductId} not found to clear auction", message.productId);
                _channel.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            product.AuctionStartTime = null;
            product.AuctionEndTime = null;

            await db.SaveChangesAsync();

            _logger.LogInformation("Cleared auction for product {ProductId} due to unverification", message.productId);
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

