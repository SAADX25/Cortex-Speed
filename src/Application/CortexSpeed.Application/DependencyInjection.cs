using CortexSpeed.Application.Services;
using CortexSpeed.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CortexSpeed.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR and its handlers from the current assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Register the core download engine (16 parallel segments)
        services.AddSingleton<IDownloadEngine, DownloadEngine>();

        return services;
    }
}
