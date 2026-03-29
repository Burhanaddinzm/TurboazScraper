using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Text.RegularExpressions;

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

    public static void SetInputIfPresent(
        this WebDriverWait wait,
        By by,
        int? value,
        string label)
    {
        if (!value.HasValue)
            return;

        var input = wait.Until(ExpectedConditions.ElementIsVisible(by));
        wait.Until(_ => input.Enabled);

        Console.WriteLine($"[SET] {label} -> {value.Value}");
        input.Clear();
        input.SendKeys(value.Value.ToString());
    }

    public static void OpenDropdown(
        this WebDriverWait wait,
        IWebElement dropdown)
    {
        if ((dropdown.GetAttribute("class") ?? "").Contains("is-open"))
            return;

        var selected = dropdown.FindElement(By.CssSelector(".tz-dropdown__selected"));
        SafeClick(dropdown, selected);

        wait.Until(_ => (dropdown.GetAttribute("class") ?? "").Contains("is-open"));
    }

    public static void CloseDropdownIfOpen(
        this WebDriverWait wait,
        IWebElement dropdown)
    {
        if (!((dropdown.GetAttribute("class") ?? "").Contains("is-open")))
            return;

        var selected = dropdown.FindElement(By.CssSelector(".tz-dropdown__selected"));
        SafeClick(dropdown, selected);

        wait.Until(_ => !((dropdown.GetAttribute("class") ?? "").Contains("is-open")));
    }

    public static void SelectSingleDropdownValue(
        this WebDriverWait wait,
        string dataId,
        string value)
    {
        var dropdown = wait.Until(d =>
            d.FindElement(By.CssSelector($"div.tz-dropdown[data-id='{dataId}']")));

        wait.OpenDropdown(dropdown);

        var option = wait.Until(_ =>
            dropdown.FindElements(By.CssSelector(".tz-dropdown__option"))
                .FirstOrDefault(el =>
                    string.Equals(el.GetAttribute("data-val")?.Trim(), value, StringComparison.OrdinalIgnoreCase)));

        if (option == null)
            throw new NoSuchElementException($"Dropdown option '{value}' not found for '{dataId}'");

        if (!(option.GetAttribute("class") ?? "").Contains("is-selected"))
        {
            var clickable = option.FindElements(By.CssSelector(".tz-dropdown__option-label")).FirstOrDefault() ?? option;
            SafeClick(dropdown, clickable);
        }

        wait.Until(_ =>
        {
            var valuesEl = dropdown.FindElement(By.CssSelector(".tz-dropdown__values"));
            return valuesEl.Text.Trim() == value;
        });

        wait.CloseDropdownIfOpen(dropdown);
    }

    public static void SetCheckbox(
        this WebDriverWait wait,
        By by,
        bool shouldBeChecked)
    {
        var checkbox = wait.Until(ExpectedConditions.ElementExists(by));

        bool isChecked = checkbox.Selected;
        if (isChecked == shouldBeChecked)
            return;

        var id = checkbox.GetAttribute("id");
        var label = wait.Until(ExpectedConditions.ElementToBeClickable(
            By.CssSelector($"label[for='{id}']")));

        SafeClick(checkbox, label);
        wait.Until(_ => checkbox.Selected == shouldBeChecked);
    }

    public static void SetMultiSelectValuesByText(
        this WebDriverWait wait,
        string selectId,
        IReadOnlyCollection<string> wantedTexts)
    {
        if (wantedTexts == null || wantedTexts.Count == 0)
            return;

        var select = wait.Until(d => d.FindElement(By.Id(selectId)));
        var driver = ((IWrapsDriver)select).WrappedDriver;

        Console.WriteLine($"[DBG] {selectId} tag = {select.TagName}");

        var jsResult = ((IJavaScriptExecutor)driver).ExecuteScript(
            """
        const select = arguments[0];
        const wanted = arguments[1];

        const normalize = s => (s || '').trim().toLowerCase();

        if (!select || select.tagName.toLowerCase() !== 'select') {
            return { ok: false, selected: [], available: [], matched: [] };
        }

        const available = Array.from(select.options).map(o => o.text.trim());
        const availableNormalized = available.map(normalize);

        const matched = [];
        const wantedNormalized = wanted.map(normalize);

        for (const option of select.options) {
            const optionText = normalize(option.text);

            const isMatch = wantedNormalized.some(w =>
                optionText === w || optionText.includes(w) || w.includes(optionText)
            );

            option.selected = isMatch;

            if (isMatch) {
                matched.push(option.text.trim());
            }
        }

        select.dispatchEvent(new Event('input', { bubbles: true }));
        select.dispatchEvent(new Event('change', { bubbles: true }));

        return {
            ok: true,
            selected: Array.from(select.selectedOptions).map(o => o.text.trim()),
            available,
            matched
        };
        """,
            select,
            wantedTexts.ToArray());

        if (jsResult is not Dictionary<string, object> result)
            throw new InvalidOperationException("Unexpected JS result while selecting markets.");

        var ok = result.TryGetValue("ok", out var okObj) && okObj is bool okValue && okValue;

        var selected = result.TryGetValue("selected", out var selectedObj) && selectedObj is IEnumerable<object> selectedEnumerable
            ? selectedEnumerable.Select(x => x?.ToString() ?? string.Empty).ToList()
            : new List<string>();

        var available = result.TryGetValue("available", out var availableObj) && availableObj is IEnumerable<object> availableEnumerable
            ? availableEnumerable.Select(x => x?.ToString() ?? string.Empty).ToList()
            : new List<string>();

        if (!ok)
        {
            throw new InvalidOperationException(
                $"Element '{selectId}' is not a <select>. Available raw options: {string.Join(", ", available)}");
        }

        foreach (var market in wantedTexts)
        {
            var matched = available.Any(s =>
                s.Equals(market, StringComparison.OrdinalIgnoreCase) ||
                s.Contains(market, StringComparison.OrdinalIgnoreCase) ||
                market.Contains(s, StringComparison.OrdinalIgnoreCase));

            if (matched)
                Console.WriteLine($"[SET] Market -> {market}");
            else
                Console.WriteLine($"[WARN] Market option not found, skipping -> {market}");
        }

        if (selected.Count == 0)
        {
            throw new NoSuchElementException(
                $"None of the requested market options were found. " +
                $"Requested: {string.Join(", ", wantedTexts)}. " +
                $"Available: {string.Join(", ", available)}");
        }

        Console.WriteLine($"[OK] Selected markets -> {string.Join(", ", selected)}");
    }

    private static void SafeClick(IWebElement context, IWebElement element)
    {
        try
        {
            element.Click();
        }
        catch
        {
            var wrapped = context as IWrapsDriver;
            var driver = wrapped?.WrappedDriver;

            if (driver is IJavaScriptExecutor js)
            {
                js.ExecuteScript("arguments[0].click();", element);
            }
            else
            {
                throw;
            }
        }
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
        var match = Regex.Match(url, @"/autos/(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }
}