using Newtonsoft.Json;
using STIN_Burza.Models;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace STIN_Burza.Services
{
    public class StockService : IStockService
    {
        protected virtual string filePath { get; } = "App_Data/favorite_stocks.json";

        // nacte oblibene ze souboru
        public List<Stock> LoadFavoriteStocks()
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<Stock>>(json) ?? [];
            }
            return []; //kdyz neexistuje tak vrati prazdny
        }

        public void SaveFavoriteStocks(List<Stock> stocks)
        {
            var json = JsonConvert.SerializeObject(stocks, Formatting.Indented);
            System.IO.File.WriteAllText(filePath, json); // Uloží zpět do souboru
        }

    }
}
