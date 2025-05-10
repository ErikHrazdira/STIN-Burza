using STIN_Burza.Models;
using System.Text.Json;

namespace STIN_Burza.Services
{
    public class ExternalApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;
        private readonly string _apiUrl;
        private readonly int _apiPort;
        private readonly string _listStockEndpoint;

        public ExternalApiService(HttpClient httpClient, IConfiguration configuration, Logger logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            _apiUrl = _configuration["ExternalApi:Url"] ?? string.Empty;
            _apiPort = _configuration.GetValue<int?>("ExternalApi:Port") ?? 0;
            _listStockEndpoint = _configuration["ExternalApi:ListStockEndpoint"] ?? string.Empty;


            if (!string.IsNullOrEmpty(_apiUrl) && _apiPort > 0)
            {
                _httpClient.BaseAddress = new Uri($"{_apiUrl}:{_apiPort}");
            }
            else
            {
                logger.Log("URL externího API nebo port není nastaven v konfiguraci.");
            }
        }

        public async Task SendPassingStockNames(List<string> passingStockNames)
        {
            if (string.IsNullOrEmpty(_apiUrl) || _apiPort == 0 || string.IsNullOrEmpty(_listStockEndpoint))
            {
                _logger.Log("Nelze odeslat data kvůli chybné konfiguraci externího API.");
                return;
            }

            var transactions = new List<StockTransaction>();
            foreach (var name in passingStockNames)
            {
                transactions.Add(new StockTransaction(name, DateTime.Now));
            }

            try
            {
                _logger.Log($"Odesílám data na {_httpClient.BaseAddress}/{_listStockEndpoint}");
                _logger.Log($"Odesílaná data: {JsonSerializer.Serialize(transactions)}");

                // Simulace odeslání
                //var response = await _httpClient.PostAsJsonAsync($"/{_listStockEndpoint}", transactions);
                //response.EnsureSuccessStatusCode();

                _logger.Log("Simulace úspěšného odeslání dat.");
            }
            catch (Exception ex)
            {
                _logger.Log($"Chyba při odesílání dat: {ex.Message}");
            }
        }
    }
}
