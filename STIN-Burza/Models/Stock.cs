namespace STIN_Burza.Models
{
    public class Stock
    {
        public string Name { get; set; }
        public List<StockPrice> PriceHistory { get; set; }

        public Stock(string name)
        {
            Name = name;
            PriceHistory = new List<StockPrice>();
        }

        // Přidání ceny k historii
        public void AddPrice(DateTime date, double price)
        {
            PriceHistory.Add(new StockPrice(date, price));
        }
    }

    // Cena pro jednu položku (datum a cena)
    public class StockPrice
    {
        public DateTime Date { get; set; }
        public double Price { get; set; }

        public StockPrice(DateTime date, double price)
        {
            Date = date;
            Price = price;
        }
    }
}
