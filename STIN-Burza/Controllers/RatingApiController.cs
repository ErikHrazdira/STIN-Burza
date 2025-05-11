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
        private readonly Logger _logger;
        private readonly ExternalApiService _externalApiService;
        private readonly IConfiguration _configuration;

        public RatingApiController(Logger logger, ExternalApiService externalApiService, IConfiguration configuration)
        {
            _logger = logger;
            _externalApiService = externalApiService;
            _configuration = configuration;
        }

        [HttpPost("rating")]
        public async Task<IActionResult> ReceiveRating([FromBody] JsonElement receivedTransactionsElement)
        {
            if (receivedTransactionsElement.ValueKind != JsonValueKind.Array)
            {
                _logger.Log("Přijatá data nejsou ve formátu JSON pole.");
                return BadRequest("Očekáváno JSON pole s hodnoceními.");
            }

            var validTransactions = new List<StockTransaction>();
            var invalidTransactionsCount = 0;

            foreach (var element in receivedTransactionsElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    _logger.Log("Přijatý prvek pole není JSON objekt.");
                    invalidTransactionsCount++;
                    continue;
                }

                var propertyCount = 0;
                string name = null;
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
                        _logger.Log($"Přijato hodnocení pro '{name}' s neplatnými daty (defaultní hodnota nebo mimo rozsah). Položka přeskočena.");
                        invalidTransactionsCount++;
                    }
                }
                else
                {
                    _logger.Log($"Přijat objekt s {propertyCount} vlastnostmi (očekáváno 4). Položka přeskočena.");
                    invalidTransactionsCount++;
                }
            }

            if (invalidTransactionsCount > 0)
            {
                _logger.Log($"Přeskočeno {invalidTransactionsCount} neplatných položek s hodnocením.");
            }

            if (validTransactions.Any())
            {
                _logger.Log($"Odesílám doporučení k prodeji na {_externalApiService.HttpClient?.BaseAddress}/{_externalApiService.SendSellRecommendationEndpoint}: {JsonSerializer.Serialize(validTransactions)}");
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