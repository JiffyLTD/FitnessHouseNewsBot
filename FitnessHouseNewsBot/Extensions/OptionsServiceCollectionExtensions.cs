using FitnessHouseNewsBot.Options;
using Microsoft.Extensions.Options;

namespace FitnessHouseNewsBot.Extensions;

public static class OptionsServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .BindOptions<ParserOptions>(
                configuration,
                ParserOptions.SectionName)
            .Validate(
                options => options.ClubKeywords.Any(keyword =>
                    !string.IsNullOrWhiteSpace(keyword)),
                "Parser:ClubKeywords must contain at least one non-empty keyword")
            .Validate(
                options => options.AlertKeywords.Any(keyword =>
                    !string.IsNullOrWhiteSpace(keyword)),
                "Parser:AlertKeywords must contain at least one non-empty keyword")
            .ValidateOnStart();

        services
            .BindOptions<VkOptions>(
                configuration,
                VkOptions.SectionName)
            .ValidateOnStart();

        services
            .BindOptions<UiLockOptions>(
                configuration,
                UiLockOptions.SectionName)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Password),
                "UiLock:Password must be configured")
            .ValidateOnStart();

        return services;
    }

    public static OptionsBuilder<TOptions> BindOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        return services
            .AddOptions<TOptions>()
            .Bind(configuration.GetRequiredSection(sectionName))
            .ValidateDataAnnotations();
    }
}
