using FitnessHouseNewsBot.Options;
using FitnessHouseNewsBot.Services;
using Microsoft.Extensions.Options;

namespace FitnessHouseNewsBot.Background;

public class ParserBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ParserBackgroundService> _logger;
    private readonly ParserOptions _options;

    public ParserBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<ParserOptions> options,
        ILogger<ParserBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Background parser started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope =
                    _serviceProvider.CreateScope();

                var parser = scope.ServiceProvider
                    .GetRequiredService<FitnessParser>();

                await parser.ParseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Parser execution failed");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(
                    _options.IntervalMinutes),
                stoppingToken);
        }
    }
}