using Microsoft.AspNetCore.Mvc;
using STIN_Burza.Models;
using STIN_Burza.Services;


namespace STIN_Burza.Controllers
{
    public class StockController : Controller
    {
        private StockService stockService = new StockService();

        public IActionResult Index()
        {
            var stocks = stockService.LoadFavoriteStocks();
            if (stocks == null)
            {
                stocks = new List<Stock>(); // Pokud je seznam null, nastavíme prázdný seznam
            }
            return View(stocks);
        }
    }
}
