using Microsoft.Extensions.DependencyInjection;
using RushToPurchase.Application.Services;
using RushToPurchase.Domain.Interfaces;
using RushToPurchase.Domain.SharedKernel.Interfaces;
using RushToPurchase.Infra.Data;
using RushToPurchase.Infra.Data.Mq;

namespace RushToPurchase.Application;

public static class DependencyInjectionExtensions
{
    public static void AddRepoService(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
    }
    
    public static void AddRabbitMqService(this IServiceCollection services)
    {
        services.AddSingleton<IRabbitmqClient, RabbitmqClient>();
    }
    
    #region 注入应用层Services

    public static void AddApplicationService(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserService, UserFsqlService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderService, OrderFsqlService>();
        
        services.AddHostedService<ConsumeRabbitMqHostedService>();
    }
    #endregion
}