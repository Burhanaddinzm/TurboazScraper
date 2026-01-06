using OpenQA.Selenium;

namespace TurboScraper;

public class Program
{
    public static void Main(string[] args)
    {
        const string FILTERED_URL = "https://turbo.az/autos?q%5Bsort%5D=&q%5Bmake%5D%5B%5D=&q%5Bmodel%5D%5B%5D=&q%5Bused%5D=&q%5Bregion%5D%5B%5D=&q%5Bprice_from%5D=&q%5Bprice_to%5D=12000&q%5Bcurrency%5D=azn&q%5Bloan%5D=0&q%5Bbarter%5D=0&q%5Bcategory%5D%5B%5D=&q%5Byear_from%5D=&q%5Byear_to%5D=&q%5Bcolor%5D%5B%5D=&q%5Bfuel_type%5D%5B%5D=&q%5Bgear%5D%5B%5D=&q%5Btransmission%5D%5B%5D=&q%5Bengine_volume_from%5D=&q%5Bengine_volume_to%5D=&q%5Bpower_from%5D=&q%5Bpower_to%5D=&q%5Bmileage_from%5D=&q%5Bmileage_to%5D=250000&q%5Bonly_shops%5D=&q%5Bprior_owners_count%5D%5B%5D=&q%5Bseats_count%5D%5B%5D=&q%5Bmarket%5D%5B%5D=&q%5Bmarket%5D%5B%5D=7&q%5Bcrashed%5D=1&q%5Bpainted%5D=1&q%5Bfor_spare_parts%5D=0&q%5Bavailability_status%5D=";

        using var scraper = new TurboazScraper(url: FILTERED_URL, isHeadless: true);
        List<IWebElement> listings = scraper.GetListings();

        foreach (IWebElement item in listings)
        {
            Console.WriteLine(item);
        }
    }
}