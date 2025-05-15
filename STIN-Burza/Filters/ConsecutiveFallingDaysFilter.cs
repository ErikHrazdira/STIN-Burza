using STIN_Burza.Models;

namespace STIN_Burza.Filters
{
    public class ConsecutiveFallingDaysFilter : IStockFilter
    {
        private readonly int _fallingDaysThreshold;

        public ConsecutiveFallingDaysFilter(IConfiguration configuration)
        {
            _fallingDaysThreshold = configuration
                .GetSection("StockFilters:ConsecutiveFallingDays")
                .GetValue<int>("Threshold", 3);
        }

        public bool ShouldFilterOut(Stock stock)
        {
            if (stock.PriceHistory == null || stock.PriceHistory.Count < _fallingDaysThreshold)
            {
                return false;
            }

            var history = stock.PriceHistory.OrderByDescending(p => p.Date).ToList();

            int consecutiveFallingDays = 0;

            for (int i = 0; i < _fallingDaysThreshold; i++)
            {
                if (i + 1 < history.Count && history[i].Price < history[i + 1].Price)
                {
                    consecutiveFallingDays++;
                }
                else
                {
                    break;
                }
            }

            return consecutiveFallingDays >= _fallingDaysThreshold;
        }
    }
}