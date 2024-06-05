using BlazorDatasheet.Portal;
using BlazorDatasheet.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlazorDatasheet.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlazorDatasheet(this IServiceCollection services)
    {
        services.AddTransient<IWindowEventService, WindowEventService>();
        services.AddScoped<IMenuService, MenuService>();
        services.TryAddScoped<PortalService>();
        return services;
    }
}