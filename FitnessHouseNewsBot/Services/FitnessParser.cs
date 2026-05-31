using System.Text.Json;
using FitnessHouseNewsBot.Models;
using FitnessHouseNewsBot.Options;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;

namespace FitnessHouseNewsBot.Services;

public class FitnessParser
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly VkService _vkService;
    private readonly ParserState _state;
    private readonly ILogger<FitnessParser> _logger;
    private readonly ParserOptions _options;

    private readonly HashSet<string> _sentMessages;

    private readonly string _storagePath =
        Path.Combine("storage", "sent-news.json");

    public FitnessParser(
        IHttpClientFactory httpFactory,
        VkService vkService,
        ParserState state,
        IOptions<ParserOptions> options,
        ILogger<FitnessParser> logger)
    {
        _httpFactory = httpFactory;
        _vkService = vkService;
        _state = state;
        _logger = logger;
        _options = options.Value;

        Directory.CreateDirectory("storage");

        _logger.LogInformation(
            "Initializing FitnessParser");

        _sentMessages = LoadSentMessages();

        _logger.LogInformation(
            "Loaded {Count} saved messages",
            _sentMessages.Count);
    }

    public async Task ParseAsync()
    {
        if (_state.IsRunning)
        {
            _logger.LogWarning(
                "Parser already running");

            return;
        }

        _state.IsRunning = true;

        var startedAt = DateTime.Now;

        try
        {
            _logger.LogInformation(
                "Parsing started at {StartedAt}",
                startedAt);

            var client = _httpFactory.CreateClient();

            _logger.LogInformation(
                "Requesting page: {Url}",
                _options.Url);

            var html =
                await client.GetStringAsync(_options.Url);

            _logger.LogInformation(
                "HTML loaded successfully. Length: {Length}",
                html.Length);

            var doc = new HtmlDocument();

            doc.LoadHtml(html);

            var articles = doc.DocumentNode
                .SelectNodes("//article[contains(@class,'news-item')]");

            if (articles == null)
            {
                _logger.LogWarning(
                    "No articles found");

                return;
            }

            _logger.LogInformation(
                "Found {Count} articles",
                articles.Count);

            foreach (var article in articles)
            {
                var clubNode = article
                    .SelectSingleNode(
                        ".//div[contains(@class,'news-item__club')]//h3");

                var textNode = article
                    .SelectSingleNode(
                        ".//div[contains(@class,'news-item__text')]");

                if (clubNode == null || textNode == null)
                {
                    _logger.LogWarning(
                        "Article skipped because required nodes are missing");

                    continue;
                }

                var club = HtmlEntity.DeEntitize(
                    clubNode.InnerText.Trim());

                var clubMatched =
                    _options.ClubKeywords.Any(keyword =>
                        club.Contains(
                            keyword,
                            StringComparison.OrdinalIgnoreCase));

                if (!clubMatched)
                {
                    _logger.LogDebug(
                        "Club skipped: {Club}",
                        club);

                    continue;
                }

                var text = HtmlEntity.DeEntitize(
                    textNode.InnerText.Trim());

                var alertMatched =
                    _options.AlertKeywords.Any(keyword =>
                        text.Contains(
                            keyword,
                            StringComparison.OrdinalIgnoreCase));

                if (!alertMatched)
                {
                    _logger.LogInformation(
                        "News skipped because no alert keywords found for {Club}",
                        club);

                    continue;
                }

                _logger.LogInformation(
                    "Alert keyword matched for club {Club}",
                    club);

                var message =
                    $"🏋️ {club}\n\n{text}";

                if (_sentMessages.Contains(message))
                {
                    _logger.LogInformation(
                        "Duplicate skipped: {Club}",
                        club);

                    continue;
                }

                _logger.LogInformation(
                    "Sending VK message for {Club}",
                    club);

                await _vkService.SendMessageAsync(message);

                _logger.LogInformation(
                    "VK message successfully sent for {Club}",
                    club);

                _sentMessages.Add(message);

                await SaveSentMessagesAsync(message);

                _logger.LogInformation(
                    "Message saved to local storage");
            }

            _state.LastRun = DateTime.Now;
            _state.LastStatus = "Успешно";

            var duration = DateTime.Now - startedAt;

            _logger.LogInformation(
                "Parsing completed successfully in {DurationMs} ms",
                duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _state.LastStatus = ex.Message;

            _logger.LogError(
                ex,
                "Parsing failed");
        }
        finally
        {
            _state.IsRunning = false;

            _logger.LogInformation(
                "Parser lock released");
        }
    }

    private HashSet<string> LoadSentMessages()
    {
        try
        {
            if (!File.Exists(_storagePath))
            {
                _logger.LogInformation(
                    "Storage file not found. Creating empty state");

                return new HashSet<string>();
            }

            var json = File.ReadAllText(_storagePath);

            var entries =
                JsonSerializer.Deserialize<List<SentNewsEntry>>(json);

            if (entries == null)
            {
                _logger.LogWarning(
                    "Storage file is empty or invalid");

                return new HashSet<string>();
            }

            _logger.LogInformation(
                "Loaded {Count} entries from storage",
                entries.Count);

            return entries
                .Select(x => x.Message)
                .ToHashSet();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load sent messages");

            return new HashSet<string>();
        }
    }

    private async Task SaveSentMessagesAsync(string lastMessage)
    {
        try
        {
            var entries = _sentMessages
                .Select(x => new SentNewsEntry
                {
                    Message = x,
                    SavedAt = DateTime.Now
                })
                .ToList();

            var json = JsonSerializer.Serialize(
                entries,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            await File.WriteAllTextAsync(
                _storagePath,
                json);

            _logger.LogInformation(
                "Saved {Count} messages to {Path}",
                entries.Count,
                _storagePath);

            _logger.LogDebug(
                "Last saved message: {Message}",
                lastMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to save sent messages");
        }
    }
    
    public async Task SendManualMessageAsync(string message)
    {
        try
        {
            _logger.LogInformation(
                "Manual resend started");

            await _vkService.SendMessageAsync(message);

            _logger.LogInformation(
                "Manual resend completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Manual resend failed");
        }
    }

    private class SentNewsEntry
    {
        public string Message { get; set; } = string.Empty;

        public DateTime SavedAt { get; set; }
    }
}