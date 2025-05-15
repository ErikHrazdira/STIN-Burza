using STIN_Burza.Models;

namespace STIN_Burza.Filters
{
    public class PriceDropsInLastWindowFilter : IStockFilter
    {
        private readonly int _dropCountThreshold;
        private readonly int _lookbackDays;

        public PriceDropsInLastWindowFilter(IConfiguration configuration)
        {
            var priceDropsConfig = configuration.GetSection("StockFilters:PriceDropsInWindow");
            _dropCountThreshold = priceDropsConfig.GetValue<int>("DropCountThreshold", 3);
            _lookbackDays = priceDropsConfig.GetValue<int>("LookbackDays", 5);
        }

        public bool ShouldFilterOut(Stock stock)
        {
            if (stock.PriceHistory == null || stock.PriceHistory.Count < 2)
            {
                return false;
            }

            var history = stock.PriceHistory.OrderByDescending(p => p.Date).Take(_lookbackDays).ToList();

            int priceDrops = 0;

            for (int i = 0; i < history.Count - 1; i++)
            {
                if (history[i].Price < history[i + 1].Price)
                {
                    priceDrops++;
                }
            }

            return priceDrops >= _dropCountThreshold;
        }
    }
}