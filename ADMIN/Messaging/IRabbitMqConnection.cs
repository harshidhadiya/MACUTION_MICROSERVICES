using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ADMIN.Messaging;

public interface IRabbitMqConnection
{
    IConnection Connection { get; }
    public void Dispose();
}

public sealed class RabbitMqConnection : IRabbitMqConnection, IDisposable
{
    private readonly IConnection _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options)
    {
        var opt = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = opt.HostName,
            Port = opt.Port,
            UserName = opt.UserName,
            Password = opt.Password,
            VirtualHost = opt.VirtualHost,
            DispatchConsumersAsync = true
        };
        _connection = factory.CreateConnection();
    }

    public IConnection Connection => _connection;

    public void Dispose()
    {
        if (_connection.IsOpen) _connection.Close();
        _connection.Dispose();
    }
}
