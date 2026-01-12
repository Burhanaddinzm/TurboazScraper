using OpenQA.Selenium;

namespace TurboScraper;

public static class Helpers
{
    public static CarModel GetCarObj(
        this IWebElement element,
        string link,
        int views,
        string transmission)
    {
        var bottomPart = element.FindElement(By.CssSelector(".products-i__bottom"));

        string dateCityText = bottomPart.FindElement(By.CssSelector(".products-i__datetime")).Text.Trim();

        string priceText = bottomPart
            .FindElement(By.CssSelector(".products-i__price"))
            .Text;
        decimal price = ParsePriceToAzn(priceText);

        string name = bottomPart.FindElement(By.CssSelector(".products-i__name")).Text;
        string details = bottomPart.FindElement(By.CssSelector(".products-i__attributes")).Text.Trim();

        var parts = dateCityText.Split(',', 2);
        string city = parts[0].Trim();
        string datePart = parts.Length > 1 ? parts[1].Trim() : "";

        DateTime date = ParseTurboDate(datePart);

        int id = ExtractIdFromUrl(link);

        return new CarModel
        {
            Id = id,
            Name = name,
            Price = price,
            Details = details,
            City = city,
            Url = link,
            Date = date,
            Views = views,
            Transmission = transmission
        };
    }

    private static decimal ParsePriceToAzn(string rawPrice)
    {
        string cleaned = rawPrice.Replace(" ", "").Trim();

        decimal rate = 1m;

        if (cleaned.Contains("€"))
            rate = 1.99m;
        else if (cleaned.Contains("$"))
            rate = 1.70m;
        else if (cleaned.Contains("₼"))
            rate = 1m;

        cleaned = cleaned
            .Replace("₼", "")
            .Replace("$", "")
            .Replace("€", "");

        if (!decimal.TryParse(cleaned, out decimal value))
            throw new FormatException($"Invalid price format: {rawPrice}");

        return Math.Round(value * rate, 2);
    }


    private static DateTime ParseTurboDate(string text)
    {
        if (text.Contains("bugün"))
        {
            var time = text.Replace("bugün", "").Trim();
            return DateTime.Today + TimeSpan.Parse(time);
        }

        if (text.Contains("dünən"))
        {
            var time = text.Replace("dünən", "").Trim();
            return DateTime.Today.AddDays(-1) + TimeSpan.Parse(time);
        }

        return DateTime.Now;
    }

    private static int ExtractIdFromUrl(string url)
    {
        var match = System.Text.RegularExpressions.Regex.Match(url, @"/autos/(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }
}
