using DotNetEnv;

namespace TurboScraper;

public class Program
{
    public static async Task Main(string[] args)
    {
        Env.Load();

        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var filteredUrl = Environment.GetEnvironmentVariable("FILTERED_URL");
        var url = string.IsNullOrWhiteSpace(filteredUrl)
            ? "https://turbo.az"
            : filteredUrl.Trim();

        var useFilters = string.IsNullOrWhiteSpace(filteredUrl);

        Console.WriteLine($"[CONFIG] URL: {url}");
        Console.WriteLine($"[CONFIG] Filters enabled: {useFilters}");

        try
        {
            using var scraper = new TurboazScraper(
                url: url,
                isHeadless: true,
                useFilters: useFilters
            );

            using var seenRepo = new SeenCarsRepository("data/turbo_scraper.db");
            using var discord = new DiscordNotifier();

            int newCount = 0;
            int existingCount = 0;

            foreach (var car in scraper.GetCars())
            {
                if (seenRepo.TryInsert(car))
                {
                    newCount++;
                    await discord.SendCarAsync(car);
                }
                else
                {
                    existingCount++;
                    seenRepo.Touch(car.Id);
                }
            }

            seenRepo.DeleteOlderThan(DateTime.UtcNow.AddDays(-30));

            Console.WriteLine($"[DONE] New: {newCount}, Existing: {existingCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}