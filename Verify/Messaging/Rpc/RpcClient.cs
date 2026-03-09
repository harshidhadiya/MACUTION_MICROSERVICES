using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VERIFY.Messaging;

namespace VERIFY.Messaging.Rpc;

public interface IRpcClient
{
    Task<TResponse?> CallAsync<TRequest, TResponse>(string exchange, string routingKey, TRequest request, CancellationToken cancellationToken = default)
        where TResponse : class;
}

public sealed class RpcClient : IRpcClient, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IConnection _rpcConnection;
    private readonly IModel _channel;
    private readonly ILogger<RpcClient> _logger;
    private readonly string _replyQueueName;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _pending = new();

    public RpcClient(IOptions<RabbitMqOptions> options, ILogger<RpcClient> logger)
    {
        _logger = logger;
        var opt = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = opt.HostName,
            Port = opt.Port,
            UserName = opt.UserName,
            Password = opt.Password,
            VirtualHost = opt.VirtualHost,
            DispatchConsumersAsync = false
        };
        _rpcConnection = factory.CreateConnection();
        _channel = _rpcConnection.CreateModel();
        _channel.ExchangeDeclare(RpcConstants.RpcExchange, ExchangeType.Direct, durable: true, autoDelete: false);

        _replyQueueName = _channel.QueueDeclare(exclusive: true, autoDelete: true).QueueName;
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (_, ea) =>
        {
            if (_pending.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                tcs.TrySetResult(ea.Body.ToArray());
        };
        _channel.BasicConsume(_replyQueueName, autoAck: true, consumer: consumer);
    }

    public async Task<TResponse?> CallAsync<TRequest, TResponse>(string exchange, string routingKey, TRequest request, CancellationToken cancellationToken = default)
        where TResponse : class
    {
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<byte[]>();
        _pending[correlationId] = tcs;

        try
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(request, JsonOptions);
            var props = _channel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = _replyQueueName;
            props.ContentType = "application/json";

            _channel.BasicPublish(exchange, routingKey, props, body);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            var responseBody = await tcs.Task.WaitAsync(cts.Token);
            return JsonSerializer.Deserialize<TResponse>(responseBody, JsonOptions);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("RPC call timed out: {RoutingKey}", routingKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RPC call failed: {RoutingKey}", routingKey);
            return null;
        }
        finally
        {
            _pending.TryRemove(correlationId, out _);
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        if (_rpcConnection?.IsOpen == true) _rpcConnection.Close();
        _rpcConnection?.Dispose();
    }
}
