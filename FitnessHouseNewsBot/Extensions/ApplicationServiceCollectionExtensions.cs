using FitnessHouseNewsBot.Background;
using FitnessHouseNewsBot.Models;
using FitnessHouseNewsBot.Services;

namespace FitnessHouseNewsBot.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationUi(
        this IServiceCollection services)
    {
        services
            .AddRazorComponents()
            .AddInteractiveServerComponents();

        return services;
    }

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        return services
            .AddHttpClient()
            .AddSingleton<ParserState>()
            .AddSingleton<UiLockService>()
            .AddSingleton<VkService>()
            .AddSingleton<FitnessParser>()
            .AddHostedService<ParserBackgroundService>();
    }
}
