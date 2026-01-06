using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace TurboScraper;

public class TurboazScraper : Scraper
{
    public TurboazScraper(string url, bool isHeadless) : base(url, isHeadless) { }

    public List<CarModel> GetCars()
    {
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

                if (dateEl == null)
                    continue;

                var dateText = dateEl.Text;

                if (!dateText.Contains("bugün") && !dateText.Contains("dünən"))
                {
                    Console.WriteLine($"[STOP] Listing out of range: {dateText}");
                    Console.WriteLine($"[DONE] Collected {cars.Count} cars");
                    return cars;
                }

                cars.Add(item.GetCarObj());
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

    // Unused
    private void AddFilters()
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

        try
        {
            Console.WriteLine("[00] Starting AddFilters");
            Console.WriteLine("[01] Waiting for More Filters button");
            var moreFiltersBtn = wait.Until(ExpectedConditions.ElementToBeClickable(
                By.CssSelector(".main-search__btn.tz-btn.tz-btn-link.tz-btn-link--primary.tz-btn-link--arrow.js-main-search-slide-down")));

            Console.WriteLine("[02] Clicking More Filters");
            moreFiltersBtn.Click();

            Console.WriteLine("[03] Waiting for Max Price input");
            var maxPriceInput = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("q_price_to")));
            wait.Until(driver => maxPriceInput.Enabled);

            Console.WriteLine("[04] Setting Max Price -> 12000");
            maxPriceInput.Clear();
            maxPriceInput.SendKeys("12000");

            Console.WriteLine("[05] Locating market dropdown");
            var dropdown = wait.Until(d => d.FindElement(By.CssSelector("div.tz-dropdown[data-id='q_market']")));
            if (!dropdown.GetAttribute("class").Contains("is-open"))
            {
                Console.WriteLine("[06] Opening dropdown");
                var toggle = dropdown.FindElement(By.CssSelector(".tz-dropdown__selected"));
                toggle.Click();
                wait.Until(d => dropdown.GetAttribute("class").Contains("is-open"));
            }

            Console.WriteLine("[07] Searching for option 'Rəsmi diler'");
            var option = wait.Until(d =>
                d.FindElements(By.CssSelector("div.tz-dropdown__option"))
                 .FirstOrDefault(el => el.Text.Trim().Contains("Rəsmi diler")));
            if (option == null) throw new NoSuchElementException("Option 'Rəsmi diler' not found");

            Console.WriteLine("[08] Clicking option");
            var label = option.FindElements(By.CssSelector("label.tz-dropdown__option-label")).FirstOrDefault();
            if (label != null) label.Click();
            else
            {
                var checkbox = option.FindElements(By.CssSelector("input[type='checkbox']")).FirstOrDefault();
                if (checkbox == null) throw new NoSuchElementException("No clickable element in option");
                checkbox.Click();
            }

            Console.WriteLine("[09] Waiting for selection to apply");
            wait.Until(d => option.GetAttribute("class")?.Contains("is-selected") == true
                             && dropdown.GetAttribute("class")?.Contains("is-selected") == true
                             && dropdown.FindElement(By.CssSelector(".tz-dropdown__values")).Text.Trim() == "Rəsmi diler");

            Console.WriteLine("[10] Closing dropdown if open");
            if (dropdown.GetAttribute("class").Contains("is-open"))
            {
                dropdown.FindElement(By.CssSelector(".tz-dropdown__selected")).Click();
                wait.Until(d => !dropdown.GetAttribute("class").Contains("is-open"));
            }

            Console.WriteLine("[11] Waiting for Mileage input");
            var mileage = wait.Until(driver => driver.FindElement(By.Id("q_mileage_to")));

            Console.WriteLine("[12] Setting Mileage -> 250000");
            mileage.Clear();
            mileage.SendKeys("250000");

            var showResultsBtn = wait.Until(ExpectedConditions.ElementToBeClickable(
                By.CssSelector("button.main-search__btn.tz-btn.tz-btn--primary[name='commit']")));
            Console.WriteLine("[13] Clicking 'Elanları göstər' button");
            showResultsBtn.Click();

            Console.WriteLine("[14] Done AddFilters");
        }
        catch (WebDriverTimeoutException)
        {
            Console.WriteLine("[ERR] Timed out waiting for element(s).");
        }
        catch (NoSuchElementException)
        {
            Console.WriteLine("[ERR] Failed to find required element(s).");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERR] Unexpected error: {e.Message}");
        }
    }
}