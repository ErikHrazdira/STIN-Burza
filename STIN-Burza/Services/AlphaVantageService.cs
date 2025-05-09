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
            try
            {
                var stock = new Stock(symbol);

                // Krok 1: Získání dnešní ceny, pokud je pracovní den
                if (DateTime.Today.DayOfWeek != DayOfWeek.Saturday && DateTime.Today.DayOfWeek != DayOfWeek.Sunday)
                {
                    var todayPrice = await GetIntradayStockPrice(symbol);
                    stock.AddPrice(DateTime.Today, todayPrice);
                }

                // Krok 2: Získání historických cen
                var previousPrices = await GetDailyStockPrices(symbol, workingDaysBack - stock.PriceHistory.Count);
                foreach (var price in previousPrices)
                {
                    stock.AddPrice(price.Date, price.Price);
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

        // Získání ceny pro dnešní den (pokud je pracovní den)
        private async Task<double> GetIntradayStockPrice(string symbol)
        {
            var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol={symbol}&interval=15min&apikey={apiKey}";
            var response = await httpClient.GetStringAsync(url);

            var data = JObject.Parse(response);
            var latestTime = data["Time Series (15min)"]?.Children<JProperty>().FirstOrDefault()?.Name;

            if (latestTime != null)
            {
                var price = data["Time Series (15min)"][latestTime]["4. close"]?.ToString();
                return price != null ? double.Parse(price, System.Globalization.CultureInfo.InvariantCulture) : 0;
            }

            throw new Exception("Could not retrieve today's intraday price.");
        }

        // Získání historických cen za poslední dny
        private async Task<List<StockPrice>> GetDailyStockPrices(string symbol, int count)
        {
            var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={apiKey}";
            var response = await httpClient.GetStringAsync(url);

            var data = JObject.Parse(response);
            var series = data["Time Series (Daily)"];

            var dates = GetPreviousWorkingDays(series, count);

            return dates.Select(date =>
            {
                var entry = series[date.ToString("yyyy-MM-dd")];
                var price = double.Parse(entry["4. close"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                return new StockPrice(date, price);
            }).ToList();
        }

        // Získání pracovních dnů z API
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
