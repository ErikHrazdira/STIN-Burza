using STIN_Burza.Models;

namespace STIN_Burza.Services
{
    public interface IAlphaVantageService
    {
        Task<Stock?> GetStockWithHistoryAsync(string symbol);
    }
}
