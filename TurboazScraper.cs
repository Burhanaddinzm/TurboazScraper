using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace TurboScraper;

public class TurboazScraper : Scraper
{
    private readonly TurboazScraperConfig _config;

    public TurboazScraper(string url, bool isHeadless) : base(url, isHeadless)
    {
        _config = TurboazScraperConfig.FromEnvironment();
    }

    public List<CarModel> GetCars()
    {
        AddFilters();
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));

        List<CarModel> cars = new List<CarModel>();
        int page = 1;

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

                var dateEl = item
                    .FindElements(By.CssSelector(".products-i__datetime"))
                    .FirstOrDefault();
                if (dateEl == null) continue;

                var dateText = dateEl.Text;

                var link = item.FindElement(By.CssSelector("a.products-i__link"))
                  .GetAttribute("href");
                if (link == null) continue;

                var (views, transmission) = GetViewCountAndTransmission(link);
                if (views >= _config.MaxViews) continue;

                if (!dateText.Contains("bugün") && !dateText.Contains("dünən"))
                {
                    Console.WriteLine($"[STOP] Listing out of range: {dateText}");
                    Console.WriteLine($"[DONE] Collected {cars.Count} cars");
                    return cars;
                }

                cars.Add(item.GetCarObj(link, views, transmission));
            }

            var nextBtn = _driver
                .FindElements(By.CssSelector(".pagination .next a"))
                .FirstOrDefault();

            if (nextBtn == null)
            {
                Console.WriteLine("[END] No next page");
                break;
            }

            var nextPageUrl = $"{_driver.Url}&page={page + 1}";
            _driver.Navigate().GoToUrl(nextPageUrl);

            wait.Until(d => d.Url.Contains($"page={page + 1}"));
            page++;
        }

        Console.WriteLine($"[DONE] Collected {cars.Count} cars");
        return cars;
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
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

        try
        {
            Console.WriteLine("[01] Starting AddFilters");

            Console.WriteLine("[02] Waiting for 'More Filters' button");
            var moreFiltersBtn = wait.Until(ExpectedConditions.ElementToBeClickable(
                By.CssSelector(".main-search__btn.tz-btn.tz-btn-link.tz-btn-link--primary.tz-btn-link--arrow.js-main-search-slide-down")));

            Console.WriteLine("[03] Clicking 'More Filters'");
            moreFiltersBtn.Click();

            Console.WriteLine("[04] Waiting for Max Price input");
            var maxPriceInput = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("q_price_to")));
            wait.Until(_ => maxPriceInput.Enabled);

            Console.WriteLine($"[05] Setting Max Price -> {_config.MaxPrice}");
            maxPriceInput.Clear();
            maxPriceInput.SendKeys(_config.MaxPrice.ToString());

            Console.WriteLine("[06] Locating Market dropdown");
            var dropdown = wait.Until(d =>
                d.FindElement(By.CssSelector("div.tz-dropdown[data-id='q_market']")));

            if (!dropdown.GetAttribute("class").Contains("is-open"))
            {
                Console.WriteLine("[07] Opening Market dropdown");
                dropdown.FindElement(By.CssSelector(".tz-dropdown__selected")).Click();
                wait.Until(d => dropdown.GetAttribute("class").Contains("is-open"));
            }

            Console.WriteLine($"[08] Selecting Market -> {_config.Market}");
            var option = wait.Until(d =>
                d.FindElements(By.CssSelector("div.tz-dropdown__option"))
                 .FirstOrDefault(el => el.Text.Trim().Contains(_config.Market)));

            if (option == null)
                throw new NoSuchElementException($"Option '{_config.Market}' not found");

            option.FindElements(By.CssSelector("label.tz-dropdown__option-label"))
                  .FirstOrDefault()?.Click();

            Console.WriteLine("[09] Waiting for Market selection to apply");
            wait.Until(d =>
                dropdown.FindElement(By.CssSelector(".tz-dropdown__values"))
                        .Text.Trim() == _config.Market);

            if (dropdown.GetAttribute("class").Contains("is-open"))
            {
                Console.WriteLine("[10] Closing Market dropdown");
                dropdown.FindElement(By.CssSelector(".tz-dropdown__selected")).Click();
                wait.Until(d => !dropdown.GetAttribute("class").Contains("is-open"));
            }

            Console.WriteLine("[11] Waiting for Mileage input");
            var mileage = wait.Until(d => d.FindElement(By.Id("q_mileage_to")));

            Console.WriteLine($"[12] Setting Mileage -> {_config.MaxMileage}");
            mileage.Clear();
            mileage.SendKeys(_config.MaxMileage.ToString());

            Console.WriteLine("[13] Clicking 'Elanları göstər'");
            var showResultsBtn = wait.Until(ExpectedConditions.ElementToBeClickable(
                By.CssSelector("button.main-search__btn.tz-btn.tz-btn--primary[name='commit']")));
            showResultsBtn.Click();

            Console.WriteLine("[14] AddFilters completed");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERR] {e.GetType().Name}: {e.Message}");
        }
    }
}