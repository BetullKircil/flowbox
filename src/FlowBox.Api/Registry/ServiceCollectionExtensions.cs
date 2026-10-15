using System.Reflection;
using System.Text;
using FlowBox.Api.Data.Ef;
using FlowBox.Api.Repositories.Auth;
using FlowBox.Api.Repositories.Courier;
using FlowBox.Api.Repositories.Order;
using FlowBox.Api.Repositories.Shipment;
using FlowBox.Api.Service;
using FlowBox.Api.Service.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FlowBox.Api.Registry;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IShipmentRepository, EfShipmentRepository>();
        services.AddScoped<ICourierRepository, EfCourierRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();

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

    /// <summary>
    /// JWT bearer authentication + authorization'ı kurar. Jwt:Secret appsettings'te
    /// tanımlı değilse uygulama açılışta (sessizce yanlış davranmak yerine) hata verir.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

        if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
        {
            throw new InvalidOperationException(
                "Jwt:Secret appsettings'te tanımlı değil. Development için appsettings.Development.json'a, " +
                "production için Jwt__Secret ortam değişkenine ya da bir secret manager'a ekleyin.");
        }

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();

        return services;
    }
}
