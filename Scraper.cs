using System.Runtime.InteropServices;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace TurboScraper;

public class Scraper : IDisposable
{
    protected readonly ChromeDriver _driver;
    private bool _disposed;

    public Scraper(string url, bool isHeadless)
    {
        var options = new ChromeOptions();

        options.PageLoadStrategy = PageLoadStrategy.Eager;
        options.AddArgument("--user-agent=Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 "
                            + "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-images");
        options.AddArgument("--blink-settings=imagesEnabled=false");

        options.AddUserProfilePreference("profile.managed_default_content_settings.images", 2);
        options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);

        ChromeDriverService service;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            options.BinaryLocation = "/usr/bin/chromium";
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--window-size=1920,1080");

            service = ChromeDriverService.CreateDefaultService("/usr/bin");
        }
        else
        {
            service = ChromeDriverService.CreateDefaultService();
        }

        if (isHeadless)
        {
            options.AddArgument("--headless");
        }

        service.SuppressInitialDiagnosticInformation = true;
        service.HideCommandPromptWindow = true;

        _driver = new ChromeDriver(service, options, TimeSpan.FromMinutes(3));
        _driver.Navigate().GoToUrl(url);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            try
            {
                _driver.Quit();
                _driver.Dispose();
            }
            catch
            {
                Console.WriteLine("Failed to dispose ChromeDriver of Selenium");
            }
        }

        _disposed = true;
    }

    ~Scraper()
    {
        Dispose(false);
    }
}