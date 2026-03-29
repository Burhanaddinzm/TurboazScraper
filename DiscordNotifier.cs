using System.Net.Http.Json;
using System.Text;

namespace TurboScraper;

public sealed class DiscordNotifier : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string? _webhookUrl;
    private bool _disposed;

    public DiscordNotifier()
    {
        _webhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL");
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task SendCarAsync(CarModel car, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
        {
            Console.WriteLine("[DISCORD] DISCORD_WEBHOOK_URL is empty. Skipping notification.");
            return;
        }

        var message = car.ToString();

        var payload = new DiscordWebhookMessage
        {
            Content = WrapAsCodeBlock(message)
        };

        using var response = await _httpClient.PostAsJsonAsync(
            _webhookUrl,
            payload,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Discord webhook failed. Status: {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
        }

        Console.WriteLine($"[DISCORD] Sent notification for car ID {car.Id}");
    }

    private static string WrapAsCodeBlock(string text)
    {
        var safe = text.Replace("```", "'''");
        return $"```text\n{safe}\n```";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient.Dispose();
        _disposed = true;
    }

    private sealed class DiscordWebhookMessage
    {
        public string Content { get; set; } = string.Empty;
    }
}