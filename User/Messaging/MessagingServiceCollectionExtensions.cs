using USER.Messaging.Consumers;
using USER.Messaging.Rpc;

namespace USER.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection("RabbitMq"))
            .ValidateOnStart();
        services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        services.AddSingleton<IRpcClient, RpcClient>();
        services.AddScoped<IAdminRpcService, AdminRpcService>();
        services.AddHostedService<RpcUserGetConsumer>();
        return services;
    }
}
