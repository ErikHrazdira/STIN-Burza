using Newtonsoft.Json.Linq;
using STIN_Burza.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace STIN_Burza.Services
{
    public class AlphaVantageDataProvider : IAlphaVantageDataProvider
    {
        private readonly string apiKey;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AlphaVantageDataProvider> _logger;

        public AlphaVantageDataProvider(IConfiguration config, ILogger<AlphaVantageDataProvider> logger, IHttpClientFactory httpClientFactory)
        {
            this.apiKey = config["AlphaVantage:ApiKey"] ?? throw new Exception("API klíč není nastaven.");
            this._httpClientFactory = httpClientFactory;
            this._logger = logger;
        }

        private HttpClient GetHttpClient()
        {
            return _httpClientFactory.CreateClient();
        }

        public async Task<double?> GetIntradayPriceAsync(string symbol)
        {
            var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol={symbol}&interval=15min&apikey={apiKey}";
            using var client = GetHttpClient();
            try
            {
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
                            return price != null ? double.Parse(price, CultureInfo.InvariantCulture) : (double?)null;
                        }
                    }
                }
                _logger.LogWarning($"Could not retrieve today's intraday price for symbol '{symbol}'. Response: {response}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving intraday price for '{symbol}': {ex.Message}");
                return null;
            }
        }

        public async Task<List<StockPrice>> GetDailyPricesAsync(string symbol, int count)
        {
            var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={apiKey}";
            using var client = GetHttpClient();
            try
            {
                var response = await client.GetStringAsync(url);
                var data = JObject.Parse(response);
                var series = data["Time Series (Daily)"];

                if (series == null)
                {
                    _logger.LogWarning($"Missing 'Time Series (Daily)' data in response for symbol '{symbol}'. Response: {response}");
                    return new List<StockPrice>();
                }

                var prices = series.Children<JProperty>()
                    .Select(p => new
                    {
                        DateString = p.Name,
                        Entry = p.Value
                    })
                    .Select(item =>
                    {
                        if (DateTime.TryParseExact(item.DateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
                            item.Entry?["4. close"]?.ToString() is string priceString &&
                            double.TryParse(priceString, NumberStyles.Float, CultureInfo.InvariantCulture, out var price))
                        {
                            return new StockPrice(date, price);
                        }
                        _logger.LogWarning($"Could not parse price for date '{item.DateString}' for symbol '{symbol}'. Entry: {item.Entry}");
                        return null;
                    })
                    .Where(price => price != null)
                    .OrderByDescending(p => p.Date)
                    .Where(p => p.Date.DayOfWeek != DayOfWeek.Saturday && p.Date.DayOfWeek != DayOfWeek.Sunday)
                    .Take(count)
                    .ToList()!;

                return prices;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving daily prices for '{symbol}': {ex.Message}");
                return new List<StockPrice>();
            }
        }
    }
}