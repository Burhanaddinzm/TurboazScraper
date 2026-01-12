using DotNetEnv;

namespace TurboScraper;

public class Program
{
    public static void Main(string[] args)
    {
        Env.Load();
        
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        const string FILTERED_URL = "https://turbo.az";

        try
        {
            using var scraper = new TurboazScraper(url: FILTERED_URL, isHeadless: true);
            List<CarModel> listings = scraper.GetCars();

            Console.WriteLine("Cars\n=======");
            foreach (CarModel car in listings) Console.WriteLine(car + "\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}