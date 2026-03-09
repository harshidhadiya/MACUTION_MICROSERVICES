using PRODUCT.Messaging.Consumers;
using PRODUCT.Messaging.Rpc;

namespace PRODUCT.Messaging;

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
        services.AddScoped<IUserRpcCaller, UserRpcCaller>();

        services.AddHostedService<ProductUnverifiedConsumer>();

        return services;
    }
}

