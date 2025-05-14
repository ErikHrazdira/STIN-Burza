using STIN_Burza.Models;

namespace STIN_Burza.Services
{
    public interface IExternalApiService
    {
        HttpClient HttpClient { get; }
        string SendSellRecommendationEndpoint { get; }
        Task SendPassingStockNames(List<string> passingStockNames);
        Task SendSellRecommendations(List<StockTransaction> recommendations);
    }
}
