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

    public async Task SendMessageAsync(string message)
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
            content);

        var responseText =
            await response.Content.ReadAsStringAsync();

        _logger.LogInformation(
            "VK response: {Response}",
            responseText);

        response.EnsureSuccessStatusCode();
    }
}