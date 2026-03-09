using VERIFY.Messaging.Consumers;
using VERIFY.Messaging.Rpc;

namespace VERIFY.Messaging;

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
        services.AddScoped<IAdminRpcCaller, AdminRpcCaller>();
        services.AddScoped<IUserRpcCaller, UserRpcCaller>();

        services.AddHostedService<ProductDeletedConsumer>();
        services.AddHostedService<ProductVerifyConsumer>();
        services.AddHostedService<createVerifyObjConsumer>();
        services.AddHostedService<ProductUnverifyConsumer>();
        services.AddHostedService<RpcVerifyStatusConsumer>();

        return services;
    }
}

