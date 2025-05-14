using Newtonsoft.Json.Linq;
using STIN_Burza.Models;
using System;


namespace STIN_Burza.Services
{
    public class AlphaVantageService : IAlphaVantageService
    {
        private readonly string apiKey;
        private readonly int workingDaysBack;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMyLogger logger;

        public AlphaVantageService(IConfiguration config, IMyLogger logger, IHttpClientFactory httpClientFactory)
        {
            this.apiKey = config["AlphaVantage:ApiKey"] ?? throw new Exception("API klíč není nastaven.");
            this.workingDaysBack = int.Parse(config["Configuration:WorkingDaysBackValues"] ?? "7");
            this.logger = logger;
            this._httpClientFactory = httpClientFactory;
        }

        private HttpClient GetHttpClient()
        {
            return _httpClientFactory.CreateClient();
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
            using var client = GetHttpClient();
            var response = await client.GetStringAsync(url);

            var data = JObject.Parse(response);
            var timeSeries = data["Time Series (15min)"];

            if (timeSeries != null)
            {
                var latestTime = timeSeries.Children<JProperty>().FirstOrDefault()?.Name;

                if (latestTime != null)
                {
                    var latestData = timeSeries[latestTime];
                    if (latestData != null)
                    {
                        var price = latestData["4. close"]?.ToString();
                        return price != null ? double.Parse(price, System.Globalization.CultureInfo.InvariantCulture) : 0;
                    }
                }
            }

            throw new Exception("Could not retrieve today's intraday price.");
        }

        // Získání historických cen za poslední dny
        private async Task<List<StockPrice>> GetDailyStockPrices(string symbol, int count)
        {
            var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={apiKey}";
            using var client = GetHttpClient();
            var response = await client.GetStringAsync(url);

            var data = JObject.Parse(response);
            var series = data["Time Series (Daily)"];

            if (series == null)
            {
                logger.Log("Chybí data 'Time Series (Daily)' v odpovědi.");
                return new List<StockPrice>();
            }

            var dates = series.Children<JProperty>()
                .Select(p => DateTime.Parse(p.Name))
                .OrderByDescending(d => d)
                .Where(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                .Take(count)
                .ToList();

            return dates.Select(date =>
            {
                var entry = series[date.ToString("yyyy-MM-dd")];
                if (entry == null || entry["4. close"] == null)
                {
                    logger.Log($"Chybí data o ceně pro datum {date:yyyy-MM-dd}.");
                    return null;
                }

                var priceString = entry["4. close"]?.ToString();
                if (!string.IsNullOrEmpty(priceString))
                {
                    var price = double.Parse(priceString, System.Globalization.CultureInfo.InvariantCulture);
                    return new StockPrice(date, price);
                }

                logger.Log($"Chybí nebo je neplatná cena pro datum {date:yyyy-MM-dd}.");
                return null;
            }).Where(price => price != null).ToList()!;
        }
    }
}