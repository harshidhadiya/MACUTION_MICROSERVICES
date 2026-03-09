using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VERIFY.Messaging.Rpc;
using VERIFY.Model;

namespace VERIFY.Messaging.Consumers;

public sealed class RpcVerifyStatusConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IRabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RpcVerifyStatusConsumer> _logger;
    private IModel? _channel;

    public RpcVerifyStatusConsumer(
        IRabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger<RpcVerifyStatusConsumer> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.Connection.CreateModel();
        _channel.ExchangeDeclare(RpcConstants.RpcExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        _channel.QueueDeclare(RpcConstants.VerifyStatusQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(RpcConstants.VerifyStatusQueue, RpcConstants.RpcExchange, RpcConstants.VerifyStatusQueue);
        _channel.BasicQos(0, 10, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) => await OnReceivedAsync(ea);
        _channel.BasicConsume(RpcConstants.VerifyStatusQueue, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task OnReceivedAsync(BasicDeliverEventArgs ea)
    {
        byte[]? replyBody = null;
        try
        {
            var request = JsonSerializer.Deserialize<VerifyStatusRequest>(ea.Body.ToArray(), JsonOptions);
            if (request?.ProductId > 0)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MACUTIONDB>();
                var record = db.VERIFY_PRODUCTS.AsNoTracking().FirstOrDefault(v => v.ProductId == request.ProductId);
                var data = record == null
                    ? new VerifyStatusData { ProductId = request.ProductId, IsVerified = false }
                    : new VerifyStatusData
                    {
                        ProductId = request.ProductId,
                        IsVerified = record.isProductVerified,
                        VerifierId = record.VerifierId,
                        VerifiedTime = record.VerifiedTime,
                        Description = record.Description
                    };
                replyBody = JsonSerializer.SerializeToUtf8Bytes(
                    new RpcResponseWrapper<VerifyStatusData> { Success = true, Data = data }, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rpc verify.status error");
            replyBody = JsonSerializer.SerializeToUtf8Bytes(
                new RpcResponseWrapper<VerifyStatusData> { Success = false, Message = ex.Message }, JsonOptions);
        }

        var replyTo = ea.BasicProperties.ReplyTo;
        if (replyBody != null && !string.IsNullOrEmpty(replyTo))
        {
            var props = _channel!.CreateBasicProperties();
            props.CorrelationId = ea.BasicProperties.CorrelationId;
            props.ContentType = "application/json";
            _channel.BasicPublish("", replyTo, props, replyBody);
        }
        _channel!.BasicAck(ea.DeliveryTag, false);
        await Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        base.Dispose();
    }
}
