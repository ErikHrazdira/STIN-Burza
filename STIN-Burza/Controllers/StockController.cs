using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using STIN_Burza.Models;
using STIN_Burza.Services;



namespace STIN_Burza.Controllers
{
    public class StockController : Controller
    {
        private readonly StockService stockService;
        private readonly Logger logger;
        private readonly AlphaVantageService alphaVantageService;

        // Konstruktory pro injektování služeb
        public StockController(StockService stockService, Logger logger, AlphaVantageService alphaVantageService)
        {
            this.stockService = stockService;
            this.logger = logger;
            this.alphaVantageService = alphaVantageService;
        }

        public IActionResult Index()
        {
            var stocks = stockService.LoadFavoriteStocks();
            if (stocks == null)
            {
                stocks = new List<Stock>(); // Pokud je seznam null, nastavíme prázdný seznam
            }
            logger.Log("Načtení oblíbených položek.");
            ViewBag.LogLines = logger.GetLastLines(); // Načte logy pro zobrazení na stránce
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

                logger.Log($"Položka '{name}' byla odstraněna z oblíbených.");
            }
            else {
                logger.Log($"Pokud položka '{name}' neexistuje v oblíbených, odstranění neprobíhá.");
            }

                return RedirectToAction("Index"); // Přesměruje zpět na hlavní stránku
        }

        [HttpPost]
        public async Task<IActionResult> AddFavorite(string name)
        {
            var existingStocks = stockService.LoadFavoriteStocks();

            // Kontrola duplicit
            if (existingStocks.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                logger.Log($"Položka '{name}' už je v oblíbených.");
                return RedirectToAction("Index");
            }

            // Získání nové akcie z API
            var newStock = await alphaVantageService.GetStockWithHistoryAsync(name);
            if (newStock == null || newStock.PriceHistory.Count == 0)
            {
                logger.Log($"Nepodařilo se přidat '{name}' – akcie nenalezena nebo neobsahuje data.");
                return RedirectToAction("Index");
            }

            // Přidání a uložení
            existingStocks.Add(newStock);
            stockService.SaveFavoriteStocks(existingStocks);
            logger.Log($"Položka '{name}' byla přidána do oblíbených.");

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAllFavorites()
        {
            var favoriteStocks = stockService.LoadFavoriteStocks();
            foreach (var stock in favoriteStocks)
            {
                var updatedStock = await alphaVantageService.GetStockWithHistoryAsync(stock.Name);
                if (updatedStock != null)
                {
                    stock.PriceHistory = updatedStock.PriceHistory; // Aktualizace historických cen
                }
            }

            stockService.SaveFavoriteStocks(favoriteStocks); // Uložení aktualizovaných dat

            return RedirectToAction("Index");
        }


    }


}
