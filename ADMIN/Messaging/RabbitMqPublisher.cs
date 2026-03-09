using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ADMIN.Messaging;

public interface IRabbitMqPublisher
{
    void Publish<T>(string routingKey, T message);
}

public sealed class RabbitMqPublisher : IRabbitMqPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IRabbitMqConnection _connection;
    private readonly RabbitMqOptions _options;

    public RabbitMqPublisher(IRabbitMqConnection connection, IOptions<RabbitMqOptions> options)
    {
        _connection = connection;
        _options = options.Value;
    }

    public void Publish<T>(string routingKey, T message)
    {
        using var channel = _connection.Connection.CreateModel();
        channel.ExchangeDeclare(exchange: _options.ExchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false);
        var json = JsonSerializer.Serialize(message, JsonOptions);
        var body = Encoding.UTF8.GetBytes(json);
        var props = channel.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2;
        channel.BasicPublish(exchange: _options.ExchangeName, routingKey: routingKey, mandatory: false, basicProperties: props, body: body);
    }
}
