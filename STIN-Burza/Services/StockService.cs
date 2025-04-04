using Newtonsoft.Json;
using STIN_Burza.Models;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace STIN_Burza.Services
{
    public class StockService
    {
        private readonly string filePath = "App_Data/favorite_stocks.json";

        // nacte oblibene ze souboru
        public List<Stock> LoadFavoriteStocks()
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<Stock>>(json) ?? new List<Stock>(); // Pokud JSON je null, vrátí prázdný seznam
            }
            return new List<Stock>(); //kdyz neexistuje tak vrati prazdny
        }

        public void SaveFavoriteStocks(List<Stock> stocks)
        {
            var json = JsonConvert.SerializeObject(stocks, Formatting.Indented);
            System.IO.File.WriteAllText(filePath, json); // Uloží zpět do souboru
        }

    }
}
