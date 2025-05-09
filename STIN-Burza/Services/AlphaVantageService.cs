using Newtonsoft.Json.Linq;
using STIN_Burza.Models;

namespace STIN_Burza.Services
{
    public class AlphaVantageService
    {
        private readonly string apiKey;
        private readonly int workingDaysBack;
        private readonly HttpClient httpClient = new();
        private readonly Logger logger;

        public AlphaVantageService(IConfiguration config, Logger logger)
        {
            this.apiKey = config["AlphaVantage:ApiKey"] ?? throw new Exception("API klíč není nastaven.");
            this.workingDaysBack = int.Parse(config["Configuration:WorkingDaysBackValues"] ?? "7");
            this.logger = logger;
        }

        public async Task<Stock?> GetStockWithHistoryAsync(string symbol)
        {
            var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={apiKey}&outputsize=compact";
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonString);

                if (!json.ContainsKey("Time Series (Daily)"))
                {
                    logger.Log($"Symbol '{symbol}' nebyl nalezen nebo API nevrátila data.");
                    return null;
                }

                var series = json["Time Series (Daily)"]!;
                var validDates = GetPreviousWorkingDays(series, workingDaysBack);

                var stock = new Stock(symbol);

                foreach (var date in validDates)
                {
                    var entry = series[date.ToString("yyyy-MM-dd")];
                    if (entry != null && entry["4. close"] != null)
                    {
                        double price = double.Parse(entry["4. close"]!.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                        stock.AddPrice(date, price);
                    }
                }

                logger.Log($"Stažena data pro symbol '{symbol}' ({stock.PriceHistory.Count} dní).");
                return stock;
            }
            catch (Exception ex)
            {
                logger.Log($"Chyba při stahování dat pro '{symbol}': {ex.Message}");
                return null;
            }
        }

        private List<DateTime> GetPreviousWorkingDays(JToken series, int count)
        {
            var allDates = series.Children<JProperty>()
                .Select(p => DateTime.Parse(p.Name))
                .OrderByDescending(d => d)
                .Where(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                .Take(count)
                .ToList();

            return allDates;
        }

    }
}
