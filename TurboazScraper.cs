using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Text.RegularExpressions;

namespace TurboScraper;

public class TurboazScraper : Scraper
{
    private readonly TurboazScraperConfig _config;
    private readonly bool _useFilters;

    public TurboazScraper(string url, bool isHeadless, bool useFilters = true)
        : base(url, isHeadless)
    {
        _config = TurboazScraperConfig.FromEnvironment();
        _useFilters = useFilters;
    }

    public IEnumerable<CarModel> GetCars()
    {
        if (_useFilters)
        {
            Console.WriteLine("[INIT] Applying filters...");
            AddFilters();
        }
        else
        {
            Console.WriteLine("[INIT] Skipping filters (FILTERED_URL mode)");
        }

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
        int page = 1;
        int yieldedCount = 0;

        while (true)
        {
            Console.WriteLine($"[PAGE] Processing page {page}");

            wait.Until(d =>
                d.FindElements(By.CssSelector(".products .products-i")).Count > 0
            );

            var items = _driver.FindElements(By.CssSelector(".products .products-i")).ToList();
            Console.WriteLine($"Found {items.Count} items");

            foreach (var item in items)
            {
                var classes = item.GetAttribute("class") ?? "";

                if (classes.Contains("vipped") ||
                    classes.Contains("featured") ||
                    classes.Contains("salon"))
                {
                    continue;
                }

                var dateEl = item.FindElements(By.CssSelector(".products-i__datetime")).FirstOrDefault();
                if (dateEl == null)
                    continue;

                var dateText = dateEl.Text;

                if (!dateText.Contains("bugün") && !dateText.Contains("dünən"))
                {
                    Console.WriteLine($"[STOP] Listing out of range: {dateText}");
                    Console.WriteLine($"[DONE] Yielded {yieldedCount} cars");
                    yield break;
                }

                var link = item.FindElement(By.CssSelector("a.products-i__link")).GetAttribute("href");
                if (string.IsNullOrWhiteSpace(link))
                    continue;

                var (views, transmission) = GetViewCountAndTransmission(link);
                if (views >= _config.MaxViews)
                    continue;

                var car = item.GetCarObj(link, views, transmission);

                if (_config.WhitelistCities.Any() &&
                    !_config.WhitelistCities.Any(c =>
                        car.City.Contains(c, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                yieldedCount++;
                yield return car;
            }

            var nextBtn = _driver
                .FindElements(By.CssSelector(".pagination .next a"))
                .FirstOrDefault();

            if (nextBtn == null)
            {
                Console.WriteLine("[END] No next page");
                break;
            }

            var nextPageUrl = BuildNextPageUrl(_driver.Url, page + 1);
            _driver.Navigate().GoToUrl(nextPageUrl);

            wait.Until(d => d.Url.Contains($"page={page + 1}"));
            page++;
        }

        Console.WriteLine($"[DONE] Yielded {yieldedCount} cars");
    }

    private static string BuildNextPageUrl(string currentUrl, int nextPage)
    {
        if (currentUrl.Contains("page="))
        {
            return Regex.Replace(
                currentUrl,
                @"([?&])page=\d+",
                $"$1page={nextPage}");
        }

        return currentUrl.Contains("?")
            ? $"{currentUrl}&page={nextPage}"
            : $"{currentUrl}?page={nextPage}";
    }

    private (int Views, string Transmission) GetViewCountAndTransmission(string listingUrl)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        var originalWindow = _driver.CurrentWindowHandle;

        ((IJavaScriptExecutor)_driver)
            .ExecuteScript("window.open(arguments[0], '_blank');", listingUrl);

        _driver.SwitchTo().Window(_driver.WindowHandles.Last());

        try
        {
            var viewsEl = wait.Until(d =>
                d.FindElements(By.CssSelector("span.product-statistics__i-text"))
                 .FirstOrDefault(e => e.Text.Contains("Baxışların sayı"))
            );

            int views = int.MaxValue;
            if (viewsEl != null)
            {
                var digits = new string(viewsEl.Text.Where(char.IsDigit).ToArray());
                int.TryParse(digits, out views);
            }

            var transmission = wait.Until(d =>
                d.FindElements(By.CssSelector(".product-properties__i"))
                 .FirstOrDefault(el =>
                     el.FindElement(By.CssSelector(".product-properties__i-name"))
                       .Text.Trim() == "Sürətlər qutusu")
            )
            ?.FindElement(By.CssSelector(".product-properties__i-value"))
            ?.Text
            ?.Trim();

            return (views, transmission ?? "Unknown");
        }
        finally
        {
            _driver.Close();
            _driver.SwitchTo().Window(originalWindow);
        }
    }

    private void AddFilters()
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));

        try
        {
            Console.WriteLine("[01] Starting AddFilters");

            Console.WriteLine("[02] Waiting for 'More Filters' button");
            var moreFiltersBtn = wait.Until(ExpectedConditions.ElementToBeClickable(
                By.CssSelector(".main-search__btn.tz-btn.tz-btn-link.tz-btn-link--primary.tz-btn-link--arrow.js-main-search-slide-down")));

            Console.WriteLine("[03] Clicking 'More Filters'");
            moreFiltersBtn.Click();

            wait.SetInputIfPresent(By.Id("q_price_from"), _config.MinPrice, "Min Price");
            wait.SetInputIfPresent(By.Id("q_price_to"), _config.MaxPrice, "Max Price");

            Console.WriteLine("[04] Setting Market values");
            wait.SetMultiSelectValuesByText("q_market", _config.Markets);

            wait.SetInputIfPresent(By.Id("q_mileage_from"), _config.MinMileage, "Min Mileage");
            wait.SetInputIfPresent(By.Id("q_mileage_to"), _config.MaxMileage, "Max Mileage");

            if (_config.YearMin.HasValue)
            {
                Console.WriteLine($"[07] Setting Year Min -> {_config.YearMin.Value}");
                wait.SelectSingleDropdownValue("q_year_from", _config.YearMin.Value.ToString());
            }

            if (_config.YearMax.HasValue)
            {
                Console.WriteLine($"[08] Setting Year Max -> {_config.YearMax.Value}");
                wait.SelectSingleDropdownValue("q_year_to", _config.YearMax.Value.ToString());
            }

            if (_config.Credit)
            {
                Console.WriteLine("[09] Enabling Credit");
                wait.SetCheckbox(By.Id("q_loan"), true);
            }

            if (_config.Barter)
            {
                Console.WriteLine("[10] Enabling Barter");
                wait.SetCheckbox(By.Id("q_barter"), true);
            }

            Console.WriteLine("[11] Clicking 'Elanları göstər'");
            var showResultsBtn = wait.Until(ExpectedConditions.ElementToBeClickable(
                By.CssSelector("button.main-search__btn.tz-btn.tz-btn--primary[name='commit']")));
            showResultsBtn.Click();

            Console.WriteLine("[12] AddFilters completed");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERR] {e.GetType().Name}: {e.Message}");
            throw;
        }
    }
}