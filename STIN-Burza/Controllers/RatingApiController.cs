using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STIN_Burza.Models;
using STIN_Burza.Services;
using System.Text.Json;

namespace STIN_Burza.Controllers
{
    [ApiController]
    [Route("")]
    public class RatingApiController : ControllerBase
    {
        private readonly IMyLogger _logger;
        private readonly IExternalApiService _externalApiService;
        private readonly IConfiguration _configuration;

        public RatingApiController(IMyLogger logger, IExternalApiService externalApiService, IConfiguration configuration)
        {
            _logger = logger;
            _externalApiService = externalApiService;
            _configuration = configuration;
        }

        [HttpPost("rating")]
        public async Task<IActionResult> ReceiveRating([FromBody] JsonElement receivedTransactionsElement)
        {
            _logger.Log("Probíhá příjem dat na API /rating");
            if (receivedTransactionsElement.ValueKind != JsonValueKind.Array)
            {
                _logger.Log("Přijatá data nejsou ve formátu JSON pole.");
                return BadRequest("Očekáváno JSON pole s hodnoceními.");
            }
            _logger.Log("Data jsou typu JSON.");

            var validTransactions = new List<StockTransaction>();
            var invalidTransactionsCount = 0;

            foreach (var element in receivedTransactionsElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    _logger.Log($"Přijatý prvek pole není JSON objekt: {element.ToString()}");
                    invalidTransactionsCount++;
                    continue;
                }

                var propertyCount = 0;
                string? name = null;
                DateTime date = DateTime.MinValue;
                int? rating = null;

                foreach (var property in element.EnumerateObject())
                {
                    propertyCount++;
                    switch (property.Name.ToLower())
                    {
                        case "name":
                            if (property.Value.ValueKind == JsonValueKind.String)
                            {
                                name = property.Value.GetString() ?? string.Empty;
                            }
                            break;
                        case "date":
                            if (property.Value.ValueKind == JsonValueKind.String && DateTime.TryParse(property.Value.GetString(), out var parsedDate))
                            {
                                date = parsedDate;
                            }
                            break;
                        case "rating":
                            if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var parsedRating))
                            {
                                rating = parsedRating;
                            }
                            break;
                    }
                }

                if (propertyCount == 4)
                {
                    if (!string.IsNullOrEmpty(name) && date != DateTime.MinValue && rating.HasValue && rating >= -10 && rating <= 10)
                    {
                        _logger.Log($"Přijata validní položka: {element.ToString()}");
                        int ratingThreshold = _configuration.GetValue<int>("Configuration:RatingThreshold");
                        int sellRecommendation = rating < ratingThreshold ? 1 : 0;
                        validTransactions.Add(new StockTransaction
                        {
                            Name = name,
                            Date = date,
                            Rating = rating,
                            Sell = sellRecommendation
                        });
                    }
                    else
                    {
                        _logger.Log($"Přijata položka s neplatnými daty: {element.ToString()}. Položka přeskočena.");
                        invalidTransactionsCount++;
                    }
                }
                else
                {
                    _logger.Log($"Přijata položka s nesprávným počtem vlastností ({propertyCount}, očekáváno 4): {element.ToString()}. Položka přeskočena.");
                    invalidTransactionsCount++;
                }
            }

            if (invalidTransactionsCount > 0)
            {
                _logger.Log($"Přeskočeno {invalidTransactionsCount} neplatných položek.");
            }

            if (validTransactions.Any())
            {
                _logger.Log($"Předávám k odeslání: {JsonSerializer.Serialize(validTransactions)}");
                await _externalApiService.SendSellRecommendations(validTransactions);
                return Ok("Hodnocení zpracována a doporučení odeslána.");
            }
            else
            {
                _logger.Log("Žádná platná hodnocení ke zpracování po provedení kontrol.");
                return Ok("Žádná platná hodnocení k odeslání doporučení.");
            }
        }
    }   
}