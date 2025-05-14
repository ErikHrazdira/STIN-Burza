using STIN_Burza.Models;

namespace STIN_Burza.Services
{
    public interface IStockFilterManager
    {
        List<string> GetPassingStockNames(List<Stock> stocks);
    }
}
