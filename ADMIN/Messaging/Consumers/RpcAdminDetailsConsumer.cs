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

public sealed class RpcAdminDetailsConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IRabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RpcAdminDetailsConsumer> _logger;
    private IModel? _channel;

    public RpcAdminDetailsConsumer(
        IRabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger<RpcAdminDetailsConsumer> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.Connection.CreateModel();
        _channel.ExchangeDeclare(RpcConstants.RpcExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        _channel.QueueDeclare(RpcConstants.AdminDetailsQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(RpcConstants.AdminDetailsQueue, RpcConstants.RpcExchange, RpcConstants.AdminDetailsQueue);
        _channel.BasicQos(0, 10, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) => await OnReceivedAsync(ea);
        _channel.BasicConsume(RpcConstants.AdminDetailsQueue, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task OnReceivedAsync(BasicDeliverEventArgs ea)
    {
        byte[]? replyBody = null;
        string? correlationId = ea.BasicProperties.CorrelationId;
        string? replyTo = ea.BasicProperties.ReplyTo;

        try
        {
            var request = JsonSerializer.Deserialize<RequestDetailsRequest>(ea.Body.ToArray(), JsonOptions);
            if (request == null || request.UserId <= 0)
            {
                replyBody = JsonSerializer.SerializeToUtf8Bytes(new RpcResponse<RequestDetailDto> { Success = false, Message = "Invalid request" }, JsonOptions);
            }
            else
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MACUTIONDB>();
                var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

                var req = db.REQUESTS.AsNoTracking().FirstOrDefault(x => x.RequestUserId == request.UserId);
                if (req == null)
                    replyBody = JsonSerializer.SerializeToUtf8Bytes(new RpcResponse<RequestDetailDto> { Success = false, Message = "Request not found" }, JsonOptions);
                else
                {
                    var dto = mapper.Map<RequestDetailDto>(req);
                    replyBody = JsonSerializer.SerializeToUtf8Bytes(new RpcResponse<RequestDetailDto> { Success = true, Data = dto }, JsonOptions);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rpc admin details error");
            replyBody = JsonSerializer.SerializeToUtf8Bytes(new RpcResponse<RequestDetailDto> { Success = false, Message = ex.Message }, JsonOptions);
        }

        if (replyBody != null && !string.IsNullOrEmpty(replyTo))
        {
            var props = _channel!.CreateBasicProperties();
            props.CorrelationId = correlationId;
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
