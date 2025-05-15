using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using STIN_Burza.Models;
using STIN_Burza.Services;



namespace STIN_Burza.Controllers
{
    public class StockController : Controller
    {
        private readonly IStockService stockService;
        private readonly IMyLogger logger;
        private readonly IAlphaVantageService alphaVantageService;
        private readonly IStockFilterManager stockFilterManager;
        private readonly IExternalApiService externalApiService;
        private readonly string _ratingThresholdFilePath;
        private const int DefaultRatingThreshold = 0;
        private readonly IConfiguration configuration;

        public StockController(IStockService stockService, IMyLogger logger, IAlphaVantageService alphaVantageService, IStockFilterManager stockFilterManager, IExternalApiService externalApiService, IConfiguration configuration, IWebHostEnvironment environment)
        {
            this.stockService = stockService;
            this.logger = logger;
            this.alphaVantageService = alphaVantageService;
            this.stockFilterManager = stockFilterManager;
            this.externalApiService = externalApiService;
            this.configuration = configuration;

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
            stocks ??= []; // Pokud je seznam null, nastavíme prázdný seznam
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
            else
            {
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
            int daysToCheck = int.TryParse(configuration["Configuration:WorkingDaysBackValues"], out var parsedValue) ? parsedValue : 7;
            var favoriteStocks = stockService.LoadFavoriteStocks();

            foreach (var stock in favoriteStocks)
            {
                if (!ShouldUpdate(stock.PriceHistory, daysToCheck))
                {
                    logger.Log($"Historická data pro akcii '{stock.Name}' jsou aktuální pro posledních {daysToCheck} pracovních dnů. Přeskakuji aktualizaci.");
                    continue;
                }

                logger.Log($"Aktualizuji historická data pro akcii '{stock.Name}'.");
                var updatedStock = await alphaVantageService.GetStockWithHistoryAsync(stock.Name);
                if (updatedStock != null)
                {
                    stock.PriceHistory = updatedStock.PriceHistory; // Aktualizace historických cen
                }
                else
                {
                    logger.Log($"Nepodařilo se získat aktualizovaná data pro akcii '{stock.Name}'.");
                }
            }

            stockService.SaveFavoriteStocks(favoriteStocks); // Uložení aktualizovaných dat

            return RedirectToAction("Index");
        }

        public static bool ShouldUpdate(List<StockPrice> history, int daysToCheck)
        {
            if (history == null || history.Count == 0)
            {
                return true; // Žádná data, je třeba aktualizovat
            }

            int checkedDays = 0;
            var currentDate = DateTime.Now.Date;

            // Kontrola pro dnešek
            if (!history.Any(p => p.Date.Date == currentDate))
            {
                return true; // Chybí data pro dnešek
            }
            checkedDays++;
            currentDate = currentDate.AddDays(-1); // Začneme kontrolovat předchozí dny

            while (checkedDays < daysToCheck)
            {
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    if (!history.Any(p => p.Date.Date == currentDate))
                    {
                        return true; // Chybí data pro alespoň jeden z předchozích pracovních dnů
                    }
                    checkedDays++;
                }
                currentDate = currentDate.AddDays(-1);
            }

            return false; // Data pro dnešek a 'daysToCheck - 1' předchozích pracovních dnů existují
        }

        [HttpPost]
        public async Task<IActionResult> StartProcess()
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
            if (passingStockNames.Count != 0)
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
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateRatingThreshold(int ratingThreshold)
        {
            if (ratingThreshold >= -10 && ratingThreshold <= 10)
            {
                try
                {
                    System.IO.File.WriteAllText(_ratingThresholdFilePath, ratingThreshold.ToString());
                }
                catch (Exception ex)
                {
                    logger.Log($"Chyba při ukládání prahu hodnocení do souboru '{_ratingThresholdFilePath}': {ex.Message}");
                }
                logger.Log($"Uživatel uložil nový práh hodnocení: {ratingThreshold}, soubor: {_ratingThresholdFilePath}");
            }
            else
            {
                logger.Log($"Uživatel se pokusil uložit neplatný práh hodnocení: {ratingThreshold}, soubor: {_ratingThresholdFilePath}");
            }
            return RedirectToAction("Index");
        }

    }


}
