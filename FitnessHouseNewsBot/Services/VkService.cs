using System.Text.Json;
using FitnessHouseNewsBot.Options;
using Microsoft.Extensions.Options;

namespace FitnessHouseNewsBot.Services;

public class VkService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VkService> _logger;
    private readonly VkOptions _options;

    public VkService(
        IHttpClientFactory httpFactory,
        IOptions<VkOptions> options,
        ILogger<VkService> logger)
    {
        _httpClient = httpFactory.CreateClient();
        _logger = logger;
        _options = options.Value;
    }

    public async Task SendMessageAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        var request = new Dictionary<string, string>
        {
            ["peer_id"] = _options.PeerId.ToString(),
            ["random_id"] = Random.Shared.NextInt64().ToString(),
            ["message"] = message,
            ["access_token"] = _options.Token,
            ["v"] = "5.199"
        };

        var content = new FormUrlEncodedContent(request);

        var response = await _httpClient.PostAsync(
            "https://api.vk.com/method/messages.send",
            content,
            cancellationToken);

        var responseText =
            await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogDebug(
            "VK response: {Response}",
            responseText);

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(responseText);

        if (!document.RootElement.TryGetProperty(
                "error",
                out var error))
        {
            _logger.LogInformation(
                "VK message accepted");

            return;
        }

        var errorCode = error.TryGetProperty("error_code", out var code)
            ? code.GetInt32()
            : 0;

        var errorMessage =
            error.TryGetProperty("error_msg", out var messageElement)
                ? messageElement.GetString()
                : "Unknown VK API error";

        throw new InvalidOperationException(
            $"VK API error {errorCode}: {errorMessage}");
    }
}
