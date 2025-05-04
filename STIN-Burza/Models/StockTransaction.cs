namespace STIN_Burza.Models
{
    //pro odesilani pres API
    public class StockTransaction
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public int? Rating { get; set; }
        public int? Sell { get; set; }

        // Konstruktor
        public StockTransaction(string name, DateTime date)
        {
            Name = name;
            Date = date;
            Rating = null;
            Sell = null;
        }
    }
}
