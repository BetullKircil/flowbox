using System.Reflection;
using FlowBox.Api.Data.Ef;
using FlowBox.Api.Repositories.Courier;
using FlowBox.Api.Repositories.Shipment;
using FlowBox.Api.Service;
using Microsoft.EntityFrameworkCore;

namespace FlowBox.Api.Registry;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IShipmentRepository, EfShipmentRepository>();
        services.AddScoped<ICourierRepository, EfCourierRepository>();

        return services;
    }
    
    public static IServiceCollection AddService(this IServiceCollection services)
    {
        var serviceTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IService).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

        foreach (var type in serviceTypes)
        {
            services.AddScoped(type);
        }

        return services;
    }

    public static IServiceCollection AddFlowBoxDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FlowBoxDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}
