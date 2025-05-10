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
        private readonly StockFilterManager stockFilterManager;
        private readonly ExternalApiService externalApiService;
        private readonly string _ratingThresholdFilePath;
        private const int DefaultRatingThreshold = 0;

        public StockController(StockService stockService, Logger logger, AlphaVantageService alphaVantageService, StockFilterManager stockFilterManager, ExternalApiService externalApiService, IConfiguration configuration, IWebHostEnvironment environment)
        {
            this.stockService = stockService;
            this.logger = logger;
            this.alphaVantageService = alphaVantageService;
            this.stockFilterManager = stockFilterManager;
            this.externalApiService = externalApiService;

            _ratingThresholdFilePath = configuration["Configuration:RatingThresholdFilePath"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "rating_threshold.txt");

            // Vytvoří soubor, pokud neexistuje a zapíše výchozí hodnotu
            if (!System.IO.File.Exists(_ratingThresholdFilePath))
            {
                try
                {
                    System.IO.File.WriteAllText(_ratingThresholdFilePath, DefaultRatingThreshold.ToString());
                }
                catch (Exception ex)
                {
                    logger.Log($"Chyba při vytváření souboru pro práh hodnocení: {ex.Message}, cesta: {_ratingThresholdFilePath}");
                }
            }
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

        [HttpPost]
    public async Task<IActionResult> RunFilters()
    {
        logger.Log("Spuštěn proces filtrování a odesílání dat.");

        var favorites = stockService.LoadFavoriteStocks();
        var historicalData = new Dictionary<Stock, List<StockPrice>>();

        foreach (var stock in favorites)
        {
            // Předpokládáme, že PriceHistory je již naplněna daty
            historicalData[stock] = stock.PriceHistory;
        }

        // Získání názvů položek, které prošly filtry
        var passingStockNames = stockFilterManager.GetPassingStockNames(favorites);

        // Odeslání dat na externí API
        if (passingStockNames.Any())
        {
            logger.Log($"Odesílám informace o položkách: {string.Join(", ", passingStockNames)} na externí API.");
            await externalApiService.SendPassingStockNames(passingStockNames);
                logger.Log("Odesílání na externí API dokončeno.");
        }
        else
        {
                logger.Log("Žádná položka neprošla všemi filtry, nic nebylo odesláno na externí API.");
        }

        ViewBag.FilteredStocks = passingStockNames;
        ViewBag.LogLines = logger.GetLastLines();
        return View("Index", favorites);
    }

        [HttpPost]
        public IActionResult UpdateRatingThreshold(int ratingThreshold)
        {
            SaveRatingThreshold(ratingThreshold);
            logger.Log($"Uživatel uložil nový práh hodnocení: {ratingThreshold}, soubor: {_ratingThresholdFilePath}");
            return RedirectToAction("Index");
        }

        private int LoadRatingThreshold()
        {
            try
            {
                string content = System.IO.File.ReadAllText(_ratingThresholdFilePath);
                if (int.TryParse(content, out int threshold))
                {
                    return threshold;
                }
                else
                {
                    logger.Log($"Nepodařilo se načíst práh hodnocení ze souboru '{_ratingThresholdFilePath}', použita výchozí hodnota: {DefaultRatingThreshold}");
                    return DefaultRatingThreshold;
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Chyba při načítání prahu hodnocení ze souboru '{_ratingThresholdFilePath}': {ex.Message}, použita výchozí hodnota: {DefaultRatingThreshold}");
                return DefaultRatingThreshold;
            }
        }

        private void SaveRatingThreshold(int threshold)
        {
            try
            {
                System.IO.File.WriteAllText(_ratingThresholdFilePath, threshold.ToString());
            }
            catch (Exception ex)
            {
                logger.Log($"Chyba při ukládání prahu hodnocení do souboru '{_ratingThresholdFilePath}': {ex.Message}");
            }
        }

    }


}
