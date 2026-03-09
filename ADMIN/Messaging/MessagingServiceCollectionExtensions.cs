using ADMIN.Messaging.Consumers;

namespace ADMIN.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection("RabbitMq"))
            .ValidateOnStart();
        services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        services.AddHostedService<RequestConsumer>();
        services.AddHostedService<RpcAdminDetailsConsumer>();
        services.AddHostedService<RpcAdminUserListConsumer>();
        services.AddHostedService<RpcAdminPendingConsumer>();
        return services;
    }
}
