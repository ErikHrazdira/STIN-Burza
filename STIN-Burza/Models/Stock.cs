namespace STIN_Burza.Models
{
    public class Stock(string name)
    {
        public string Name { get; set; } = name;
        public List<StockPrice> PriceHistory { get; set; } = [];

        // Přidání ceny k historii
        public void AddPrice(DateTime date, double price)
        {
            PriceHistory.Add(new StockPrice(date, price));
        }
    }

    // Cena pro jednu položku (datum a cena)
    public class StockPrice(DateTime date, double price)
    {
        public DateTime Date { get; set; } = date;
        public double Price { get; set; } = price;
    }
}
