using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using USER.Messaging.Rpc;
using USER.Model;

namespace USER.Messaging.Consumers;

public sealed class RpcUserGetConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IRabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RpcUserGetConsumer> _logger;
    private IModel? _channel;

    public RpcUserGetConsumer(
        IRabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger<RpcUserGetConsumer> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.Connection.CreateModel();
        _channel.ExchangeDeclare(UserRpcConstants.RpcExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        _channel.QueueDeclare(UserRpcConstants.UserGetQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(UserRpcConstants.UserGetQueue, UserRpcConstants.RpcExchange, UserRpcConstants.UserGetQueue);
        _channel.BasicQos(0, 10, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) => await OnReceivedAsync(ea);
        _channel.BasicConsume(UserRpcConstants.UserGetQueue, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task OnReceivedAsync(BasicDeliverEventArgs ea)
    {
        byte[]? replyBody = null;
        try
        {
            var request = JsonSerializer.Deserialize<UserGetRequest>(ea.Body.ToArray(), JsonOptions);
            if (request?.UserId > 0)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MACUTIONDB>();
                var user = db.USERS.AsNoTracking().FirstOrDefault(u => u.Id == request.UserId);
                if (user != null)
                {
                    var response = new RpcResponseWrapper<UserGetResponse>
                    {
                        Success = true,
                        Data = new UserGetResponse
                        {
                            Id = user.Id,
                            Name = user.Name,
                            Email = user.Email,
                            Role = user.Role,
                            Phone = user.Phone,
                            Address = user.Address,
                            ProfilePicture = user.ProfilePicture
                        }
                    };
                    replyBody = JsonSerializer.SerializeToUtf8Bytes(response, JsonOptions);
                }
            }
            if (replyBody == null)
                replyBody = JsonSerializer.SerializeToUtf8Bytes(new RpcResponseWrapper<UserGetResponse> { Success = false, Message = "User not found" }, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rpc user.get error");
            replyBody = JsonSerializer.SerializeToUtf8Bytes(new RpcResponseWrapper<UserGetResponse> { Success = false, Message = ex.Message }, JsonOptions);
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
