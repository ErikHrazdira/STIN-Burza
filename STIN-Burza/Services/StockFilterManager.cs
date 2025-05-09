using STIN_Burza.Filters;
using STIN_Burza.Models;

namespace STIN_Burza.Services
{
    public class StockFilterManager
    {
        private readonly IEnumerable<IStockFilter> _filters;
        private readonly Logger _logger;

        public StockFilterManager(IEnumerable<IStockFilter> filters, Logger logger)
        {
            _filters = filters;
            _logger = logger;
        }

        public List<string> GetPassingStockNames(List<Stock> stocks)
        {
            var passingStockNames = new List<string>();

            foreach (var stock in stocks)
            {
                bool shouldFilter = false;
                foreach (var filter in _filters)
                {
                    if (filter.ShouldFilterOut(stock))
                    {
                        _logger.Log($"Položka '{stock.Name}' neprošla filtrací kvůli: '{filter.GetType().Name}'.");
                        shouldFilter = true;
                        break;
                    }
                }

                if (!shouldFilter)
                {
                    passingStockNames.Add(stock.Name);
                }
            }

            return passingStockNames;
        }
    }
}
