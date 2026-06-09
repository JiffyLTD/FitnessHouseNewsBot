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
    private readonly IReadOnlyList<string> _clubKeywords;
    private readonly IReadOnlyList<string> _alertKeywords;
    private readonly SemaphoreSlim _parseLock = new(1, 1);

    private readonly Dictionary<string, SentNewsEntry> _sentMessages;
    private readonly string _storagePath;

    public FitnessParser(
        IHttpClientFactory httpFactory,
        VkService vkService,
        ParserState state,
        IOptions<ParserOptions> options,
        IHostEnvironment environment,
        ILogger<FitnessParser> logger)
    {
        _httpFactory = httpFactory;
        _vkService = vkService;
        _state = state;
        _logger = logger;
        _options = options.Value;
        _clubKeywords = NormalizeKeywords(_options.ClubKeywords);
        _alertKeywords = NormalizeKeywords(_options.AlertKeywords);
        _storagePath = Path.Combine(
            environment.ContentRootPath,
            "storage",
            "sent-news.json");

        Directory.CreateDirectory(
            Path.GetDirectoryName(_storagePath)!);

        _logger.LogInformation(
            "Initializing FitnessParser");

        _sentMessages = LoadSentMessages();

        _logger.LogInformation(
            "Loaded {Count} saved messages",
            _sentMessages.Count);
    }

    public async Task ParseAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_parseLock.Wait(0))
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
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "FitnessHouseNewsBot/1.0");

            _logger.LogInformation(
                "Requesting page: {Url}",
                _options.Url);

            var html =
                await client.GetStringAsync(
                    _options.Url,
                    cancellationToken);

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

                _state.LastRun = DateTime.Now;
                _state.LastStatus = "Новости не найдены";

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
                    _clubKeywords.Any(keyword =>
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
                    _alertKeywords.Any(keyword =>
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

                if (_sentMessages.ContainsKey(message))
                {
                    _logger.LogInformation(
                        "Duplicate skipped: {Club}",
                        club);

                    continue;
                }

                _logger.LogInformation(
                    "Sending VK message for {Club}",
                    club);

                await _vkService.SendMessageAsync(
                    message,
                    cancellationToken);

                _logger.LogInformation(
                    "VK message successfully sent for {Club}",
                    club);

                _sentMessages[message] = new SentNewsEntry
                {
                    Message = message,
                    SavedAt = DateTime.Now
                };

                await SaveSentMessagesAsync(cancellationToken);

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
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            _state.LastStatus = "Отменено";

            _logger.LogInformation(
                "Parsing canceled");
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
            _parseLock.Release();

            _logger.LogInformation(
                "Parser lock released");
        }
    }

    private Dictionary<string, SentNewsEntry> LoadSentMessages()
    {
        try
        {
            if (!File.Exists(_storagePath))
            {
                _logger.LogInformation(
                    "Storage file not found. Creating empty state");

                return [];
            }

            var json = File.ReadAllText(_storagePath);

            var entries =
                JsonSerializer.Deserialize<List<SentNewsEntry>>(json);

            if (entries == null)
            {
                _logger.LogWarning(
                    "Storage file is empty or invalid");

                return [];
            }

            _logger.LogInformation(
                "Loaded {Count} entries from storage",
                entries.Count);

            var sentMessages =
                new Dictionary<string, SentNewsEntry>(
                    StringComparer.Ordinal);

            foreach (var entry in entries.Where(x =>
                         !string.IsNullOrWhiteSpace(x.Message)))
            {
                if (!sentMessages.TryGetValue(
                        entry.Message,
                        out var existingEntry)
                    || entry.SavedAt > existingEntry.SavedAt)
                {
                    sentMessages[entry.Message] = entry;
                }
            }

            return sentMessages;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load sent messages");

            return [];
        }
    }

    private async Task SaveSentMessagesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var entries = _sentMessages
                .Values
                .OrderByDescending(x => x.SavedAt)
                .ToList();

            var json = JsonSerializer.Serialize(
                entries,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            await File.WriteAllTextAsync(
                _storagePath,
                json,
                cancellationToken);

            _logger.LogInformation(
                "Saved {Count} messages to {Path}",
                entries.Count,
                _storagePath);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to save sent messages");
        }
    }

    public async Task SendManualMessageAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Manual resend started");

            await _vkService.SendMessageAsync(
                message,
                cancellationToken);

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

    private static IReadOnlyList<string> NormalizeKeywords(
        IEnumerable<string> keywords)
    {
        return keywords
            .Select(keyword => keyword.Trim())
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
