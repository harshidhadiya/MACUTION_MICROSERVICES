using System.Text;
using System.Text.Json;
using ADMIN.Data.Dto;
using ADMIN.Messaging.Rpc;
using ADMIN.Model;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ADMIN.Messaging.Consumers;

public sealed class RpcAdminUserListConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IRabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RpcAdminUserListConsumer> _logger;
    private IModel? _channel;

    public RpcAdminUserListConsumer(
        IRabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger<RpcAdminUserListConsumer> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.Connection.CreateModel();
        _channel.ExchangeDeclare(RpcConstants.RpcExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        _channel.QueueDeclare(RpcConstants.AdminUserListQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(RpcConstants.AdminUserListQueue, RpcConstants.RpcExchange, RpcConstants.AdminUserListQueue);
        _channel.BasicQos(0, 10, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) => await OnReceivedAsync(ea);
        _channel.BasicConsume(RpcConstants.AdminUserListQueue, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task OnReceivedAsync(BasicDeliverEventArgs ea)
    {
        byte[]? replyBody = null;
        try
        {
            var request = JsonSerializer.Deserialize<UserListRequest>(ea.Body.ToArray(), JsonOptions);
            int userId = request?.UserId ?? 0;
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MACUTIONDB>();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

            var list = db.REQUESTS.Where(r => r.VerifierId == userId).AsNoTracking().ToList();
            var dtos = mapper.Map<List<RequestDetailDto>>(list);
            replyBody = JsonSerializer.SerializeToUtf8Bytes(new RpcResponse<List<RequestDetailDto>> { Success = true, Data = dtos }, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rpc admin userlist error");
            replyBody = JsonSerializer.SerializeToUtf8Bytes(new RpcResponse<List<RequestDetailDto>> { Success = false, Message = ex.Message }, JsonOptions);
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
