namespace TurboScraper;

using OpenQA.Selenium.Chrome;

public class Scraper : IDisposable
{
    protected readonly ChromeDriver _driver;
    private bool _disposed;

    public Scraper(string url, bool isHeadless)
    {
        var options = new ChromeOptions();
        if (isHeadless)
        {
            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--user-agent=Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 "
                                + "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        _driver = new ChromeDriver(options);
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
                System.Console.WriteLine("Failed to dispose ChromeDriver of Selenium");
            }
        }
        _disposed = true;
    }

    ~Scraper()
    {
        Dispose(false);
    }
}