using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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

        [HttpPost]
        public ActionResult RemoveFavorite(string name)
        {
            var stocks = stockService.LoadFavoriteStocks(); // Načte seznam oblíbených položek

            // Najde položku podle názvu a odstraní ji
            var stockToRemove = stocks.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (stockToRemove != null)
            {
                stocks.Remove(stockToRemove); // Odstraní položku
                stockService.SaveFavoriteStocks(stocks); // Uloží aktualizovaný seznam
            }

            return RedirectToAction("Index"); // Přesměruje zpět na hlavní stránku
        }

    }


}
