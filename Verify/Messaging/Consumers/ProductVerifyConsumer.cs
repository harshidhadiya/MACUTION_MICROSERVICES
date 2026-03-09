
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VERIFY.Messaging.Events;
using VERIFY.Model;

namespace VERIFY.Messaging.Consumers;

public class ProductVerifyConsumer : BackgroundService
{
    readonly IRabbitMqConnection connection;
    readonly ILogger<ProductVerifyConsumer> logger;
    readonly IServiceScopeFactory serviceScope;
    IModel _channel;

    public ProductVerifyConsumer(IRabbitMqConnection connection, ILogger<ProductVerifyConsumer> logger, IServiceScopeFactory serviceScope)
    {
        this.connection = connection;
        this.logger = logger;
        this.serviceScope = serviceScope;
    }
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = connection.Connection.CreateModel();
        _channel.ExchangeDeclare(exchange: "admin.exchange", type: ExchangeType.Direct, durable: true, autoDelete: false);
        const string queueName = "product.verify";
        const string routingKey = "product.verify";
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: queueName, exchange: "admin.exchange", routingKey: routingKey);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += verifyProduct;
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }
    public async Task verifyProduct(object sender, BasicDeliverEventArgs args)
    {

        using var scope = serviceScope.CreateScope();
        try
        {

            var dbContext = scope.ServiceProvider.GetRequiredService<MACUTIONDB>();
            var body = args.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            Console.WriteLine(json);
            var product = JsonSerializer.Deserialize<ProductVerifyEvent>(json);
            if (product != null)
            {
                var containProduct = dbContext.VERIFY_PRODUCTS.Where(x => x.ProductId == product.productId).FirstOrDefault();
                if (containProduct == null)
                {
                    containProduct = new VerifyProductTable
                    {
                        ProductId = product.productId,
                        SellerId = 0,
                        ProductName = string.Empty
                    };

                    await dbContext.VERIFY_PRODUCTS.AddAsync(containProduct);
                }

                containProduct.isProductVerified = true;
                containProduct.Description = product.description;
                containProduct.VerifierId = product.verifierId;
                containProduct.VerifiedTime = DateTime.Now;

                await dbContext.SaveChangesAsync();
            }
            _channel.BasicAck(args.DeliveryTag, false);

        }
        catch (System.Exception)
        {
            logger.LogError("Error processing message with DeliveryTag: {DeliveryTag}", args.DeliveryTag);
            _channel.BasicNack(args.DeliveryTag, false, true);

        }
    }
    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();

#pragma warning restore format
        base.Dispose();
    }
}