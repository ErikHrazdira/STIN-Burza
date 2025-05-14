using Newtonsoft.Json.Linq;
using STIN_Burza.Models;
using System;


namespace STIN_Burza.Services
{
    public class AlphaVantageService : IAlphaVantageService
    {
        private readonly int workingDaysBack;
        private readonly IMyLogger _logger;
        private readonly IAlphaVantageDataProvider _alphaVantageDataProvider;

        public AlphaVantageService(IConfiguration config, IMyLogger logger, IAlphaVantageDataProvider alphaVantageDataProvider)
        {
            this.workingDaysBack = int.Parse(config["Configuration:WorkingDaysBackValues"] ?? "7");
            this._logger = logger;
            this._alphaVantageDataProvider = alphaVantageDataProvider;
        }

        public async Task<Stock?> GetStockWithHistoryAsync(string symbol)
        {
            try
            {
                var stock = new Stock(symbol);

                // Krok 1: Získání dnešní ceny, pokud je pracovní den
                if (DateTime.Today.DayOfWeek != DayOfWeek.Saturday && DateTime.Today.DayOfWeek != DayOfWeek.Sunday)
                {
                    var todayPrice = await _alphaVantageDataProvider.GetIntradayPriceAsync(symbol);
                    if (todayPrice.HasValue)
                    {
                        stock.AddPrice(DateTime.Today, todayPrice.Value);
                    }
                }

                // Krok 2: Získání historických cen.
                var previousPrices = await _alphaVantageDataProvider.GetDailyPricesAsync(symbol, workingDaysBack - stock.PriceHistory.Count);
                foreach (var price in previousPrices)
                {
                    stock.AddPrice(price.Date, price.Price);
                }

                _logger.Log($"Stažena data pro symbol '{symbol}' ({stock.PriceHistory.Count} dní).");
                return stock;
            }
            catch (Exception ex)
            {
                _logger.Log($"Chyba při stahování dat pro '{symbol}': {ex.Message}");
                return null;
            }
        }
    }
}