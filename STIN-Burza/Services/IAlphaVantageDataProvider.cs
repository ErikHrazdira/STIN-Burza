using STIN_Burza.Models;

namespace STIN_Burza.Services
{
    public interface IAlphaVantageDataProvider
    {
        Task<double?> GetIntradayPriceAsync(string symbol);
        Task<List<StockPrice>> GetDailyPricesAsync(string symbol, int count);
    }
}
