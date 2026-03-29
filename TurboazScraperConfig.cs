namespace TurboScraper;

public class TurboazScraperConfig
{
    public int MaxViews { get; init; }
    public int? MinPrice { get; init; }
    public int MaxPrice { get; init; }
    public int? MinMileage { get; init; }
    public int MaxMileage { get; init; }
    public int? YearMin { get; init; }
    public int? YearMax { get; init; }
    public bool Credit { get; init; }
    public bool Barter { get; init; }
    public List<string> Markets { get; init; } = new();
    public List<string> WhitelistCities { get; init; } = new();

    public static TurboazScraperConfig FromEnvironment()
    {
        var markets = GetList("SCRAPER_MARKETS");

        return new TurboazScraperConfig
        {
            MaxViews = GetInt("SCRAPER_MAX_VIEWS", 1500),
            MinPrice = GetNullableInt("SCRAPER_MIN_PRICE"),
            MaxPrice = GetInt("SCRAPER_MAX_PRICE", 10000),
            MinMileage = GetNullableInt("SCRAPER_MIN_MILEAGE") ?? GetNullableInt("SCRAPER_MIN_MILAGE"),
            MaxMileage = GetInt("SCRAPER_MAX_MILEAGE", 250000),
            YearMin = GetNullableInt("SCRAPER_YEAR_MIN"),
            YearMax = GetNullableInt("SCRAPER_YEAR_MAX"),
            Credit = GetBool("SCRAPER_CREDIT"),
            Barter = GetBool("SCRAPER_BARTER"),
            Markets = markets.Any() ? markets : new List<string> { "Rəsmi diler" },
            WhitelistCities = GetList("SCRAPER_WHITELIST_CITY")
        };
    }

    private static int GetInt(string key, int fallback)
        => int.TryParse(Environment.GetEnvironmentVariable(key), out var v)
            ? v
            : fallback;

    private static int? GetNullableInt(string key)
        => int.TryParse(Environment.GetEnvironmentVariable(key), out var v)
            ? v
            : null;

    private static bool GetBool(string key, bool fallback = false)
    {
        var raw = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        raw = raw.Trim();

        return raw.Equals("1", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("true", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("on", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> GetList(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);

        return string.IsNullOrWhiteSpace(value)
            ? new List<string>()
            : value.Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
    }
}