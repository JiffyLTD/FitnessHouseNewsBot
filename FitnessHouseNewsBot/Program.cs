using FitnessHouseNewsBot.Background;
using FitnessHouseNewsBot.Components;
using FitnessHouseNewsBot.Models;
using FitnessHouseNewsBot.Options;
using FitnessHouseNewsBot.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services
    .AddOptions<ParserOptions>()
    .Bind(builder.Configuration.GetSection("Parser"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<VkOptions>()
    .Bind(builder.Configuration.GetSection("Vk"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<ParserState>();

builder.Services.AddSingleton<VkService>();
builder.Services.AddSingleton<FitnessParser>();

builder.Services.AddHostedService<ParserBackgroundService>();

var app = builder.Build();

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

try
{
    Log.Information("Application starting");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application crashed");
}
finally
{
    Log.CloseAndFlush();
}