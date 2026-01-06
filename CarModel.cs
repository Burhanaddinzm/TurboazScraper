namespace TurboScraper;

public class CarModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string Details { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Url { get; set; } = null!;
    public DateTime Date { get; set; }

    public CarModel()
    {
        Date = new DateTime();
    }

    public override string ToString()
    {
        return @$"City: {City}
Name: {Name}
Details: {Details}
Price: {Price} AZN
Date: {Date: dd-MM-yyyy HH:mm}
Url: {Url}
Id: {Id}";
    }
}
