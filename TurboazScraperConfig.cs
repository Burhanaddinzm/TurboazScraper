namespace TurboScraper;

public class TurboazScraperConfig
{
    public int MaxViews { get; init; }
    public int MaxPrice { get; init; }
    public int MaxMileage { get; init; }
    public string Market { get; init; } = "Rəsmi diler";

    public static TurboazScraperConfig FromEnvironment()
    {
        return new TurboazScraperConfig
        {
            MaxViews = GetInt("SCRAPER_MAX_VIEWS", 1500),
            MaxPrice = GetInt("SCRAPER_MAX_PRICE", 10000),
            MaxMileage = GetInt("SCRAPER_MAX_MILEAGE", 250000),
            Market = Environment.GetEnvironmentVariable("SCRAPER_MARKET") ?? "Rəsmi diler"
        };
    }

    private static int GetInt(string key, int fallback)
        => int.TryParse(Environment.GetEnvironmentVariable(key), out var v)
            ? v
            : fallback;
}
