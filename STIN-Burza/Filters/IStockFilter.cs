using STIN_Burza.Models;

namespace STIN_Burza.Filters
{
    public interface IStockFilter
    {
        bool ShouldFilterOut(Stock stock);
    }
}
