
using System.Text;
using System.Text.Json;
using ADMIN.Messaging.Events;
using ADMIN.Model;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ADMIN.Messaging.Consumers;
// this basically is used for the establish the create the instance of the verified admin okay 


public class RequestConsumer : BackgroundService
{
    ILogger<RequestConsumer> _logger;
    IRabbitMqConnection _connection;
    IModel _channel;
    private readonly IServiceScopeFactory scope;
    public RequestConsumer(ILogger<RequestConsumer> logger, IRabbitMqConnection connection, IServiceScopeFactory scope)
    {
        _logger = logger;
        _connection = connection;
        this.scope = scope;


    }
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.Connection.CreateModel();
        _channel.ExchangeDeclare(exchange: "admin", type: ExchangeType.Direct, durable: true, autoDelete: false);
        _channel.QueueDeclare(queue: "admin.request", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: "admin.request", exchange: "admin", routingKey: "request.created");
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += CreateRequestAsync;
        _channel.BasicConsume(queue: "admin.request", autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

   public override void Dispose()
    {
        _channel?.Close();
        _connection?.Dispose();
        base.Dispose();
    }


    public async Task CreateRequestAsync(object sender, BasicDeliverEventArgs args)
{
    try
    {
        var json = Encoding.UTF8.GetString(args.Body.ToArray());
        _logger.LogInformation("Received request.created event with body: {Json}", json);

        var correctdata = JsonSerializer.Deserialize<CreateRequest>(json);

        if (correctdata == null)
        {
            _channel.BasicNack(args.DeliveryTag, false, false);
            return;
        }
        Console.WriteLine(correctdata.requestUserId);
        using var scoped = scope.CreateScope();
        var db = scoped.ServiceProvider.GetRequiredService<MACUTIONDB>();

        var createRequest = new RequestTable
        {
            RequestUserId = correctdata.requestUserId,
            VerifiedByAdmin = false,
            VerifierId = 0,
            CreatedAt = DateTime.UtcNow,
            Name=correctdata.name,
            Email=correctdata.email
        };

        var exist_user= await db.REQUESTS.Where(x=>x.RequestUserId==correctdata.requestUserId).FirstOrDefaultAsync();
        if(exist_user!=null)
        {
            _logger.LogWarning("Request already exists for user id: {UserId}", correctdata.requestUserId);
            _channel.BasicAck(args.DeliveryTag, false);
            return;
        }

        await db.REQUESTS.AddAsync(createRequest);
        await db.SaveChangesAsync();

        _channel.BasicAck(args.DeliveryTag, false);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing request.created event");
        _channel.BasicNack(args.DeliveryTag, false, true);
    }
}
}