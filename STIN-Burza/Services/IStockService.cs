using STIN_Burza.Models;

namespace STIN_Burza.Services
{
    public interface IStockService
    {
        List<Stock> LoadFavoriteStocks();
        void SaveFavoriteStocks(List<Stock> stocks);
    }
}
