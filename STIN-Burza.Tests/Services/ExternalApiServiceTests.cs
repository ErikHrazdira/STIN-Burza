using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Configuration;
using STIN_Burza.Services;
using STIN_Burza.Models;
using System.Collections.Generic;

namespace STIN_Burza.Tests.Services
{
    public class ExternalApiServiceTests
    {
        [Fact]
        public async Task SendPassingStockNames_SendsPostRequestAndLogs()
        {
            // Arrange
            var mockLogger = new Mock<IMyLogger>();
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["ExternalApi:Url"]).Returns("http://localhost");
            mockConfig.Setup(c => c["ExternalApi:ListStockEndpoint"]).Returns("api/stocks");
            mockConfig.Setup(c => c["ExternalApi:Port"]).Returns("5000");
            var mockPortSection = new Mock<IConfigurationSection>();
            mockPortSection.Setup(x => x.Value).Returns("5000");
            mockConfig.Setup(x => x.GetSection("ExternalApi:Port")).Returns(mockPortSection.Object);



            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                })
                .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);

            var service = new ExternalApiService(httpClient, mockConfig.Object, mockLogger.Object);

            var stockNames = new List<string> { "AAPL", "GOOG" };

            // Act
            await service.SendPassingStockNames(stockNames);

            // Assert
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString().Contains("api/stocks")
                ),
                ItExpr.IsAny<CancellationToken>()
            );

            mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Odesílám data"))), Times.Once);
            mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Odesílaná data"))), Times.Once);
            mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Simulace úspěšného odeslání dat."))), Times.Once);
        }

        [Fact]
        public async Task SendPassingStockNames_InvalidConfig_LogsErrorAndDoesNotSend()
        {
            var mockLogger = new Mock<IMyLogger>();
            var mockConfig = new Mock<IConfiguration>();
            var mockPortSection = new Mock<IConfigurationSection>();
            mockPortSection.Setup(x => x.Value).Returns((string?)null);
            mockConfig.Setup(x => x.GetSection("ExternalApi:Port")).Returns(mockPortSection.Object);

            // Nezadáme URL ani endpoint
            var httpClient = new HttpClient(new Mock<HttpMessageHandler>().Object);
            var service = new ExternalApiService(httpClient, mockConfig.Object, mockLogger.Object);

            await service.SendPassingStockNames(new List<string> { "AAPL" });

            mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Nelze odeslat data kvůli chybné konfiguraci externího API."))), Times.Once);
        }


        [Fact]
        public async Task SendSellRecommendations_SendsPostRequestAndLogs()
        {
            // Arrange
            var mockLogger = new Mock<IMyLogger>();
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["ExternalApi:Url"]).Returns("http://localhost");
            mockConfig.Setup(c => c["ExternalApi:SendSellRecommendationEndpoint"]).Returns("api/sell");
            mockConfig.Setup(c => c["ExternalApi:Port"]).Returns("5000");
            var mockPortSection = new Mock<IConfigurationSection>();
            mockPortSection.Setup(x => x.Value).Returns("5000");
            mockConfig.Setup(x => x.GetSection("ExternalApi:Port")).Returns(mockPortSection.Object);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                })
                .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);

            var service = new ExternalApiService(httpClient, mockConfig.Object, mockLogger.Object);

            var recommendations = new List<StockTransaction>
    {
        new StockTransaction("AAPL", DateTime.Now) { Rating = 1, Sell = 1 },
        new StockTransaction("GOOG", DateTime.Now) { Rating = -2, Sell = 1 }
    };

            // Act
            await service.SendSellRecommendations(recommendations);

            // Assert
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString().Contains("api/sell")
                ),
                ItExpr.IsAny<CancellationToken>()
            );

            mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Odesílám doporučení k prodeji"))), Times.Once);
        }

        [Fact]
        public async Task SendSellRecommendations_InvalidConfig_LogsErrorAndDoesNotSend()
        {
            var mockLogger = new Mock<IMyLogger>();
            var mockConfig = new Mock<IConfiguration>();
            var mockPortSection = new Mock<IConfigurationSection>();
            mockPortSection.Setup(x => x.Value).Returns("5000");
            mockConfig.Setup(x => x.GetSection("ExternalApi:Port")).Returns(mockPortSection.Object);

            // Chybí URL a endpoint
            var httpClient = new HttpClient(new Mock<HttpMessageHandler>().Object);
            var service = new ExternalApiService(httpClient, mockConfig.Object, mockLogger.Object);

            await service.SendSellRecommendations(new List<StockTransaction> { new StockTransaction("AAPL", DateTime.Now) });

            mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Nelze odeslat doporučení k prodeji kvůli chybějící konfiguraci API pro odeslání doporučení."))), Times.Once);
        }

        [Fact]
        public async Task SendSellRecommendations_HttpThrows_LogsError()
        {
            var mockLogger = new Mock<IMyLogger>();
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["ExternalApi:Url"]).Returns("http://localhost");
            mockConfig.Setup(c => c["ExternalApi:SendSellRecommendationEndpoint"]).Returns("api/sell");
            mockConfig.Setup(c => c["ExternalApi:Port"]).Returns("5000");
            var mockPortSection = new Mock<IConfigurationSection>();
            mockPortSection.Setup(x => x.Value).Returns("5000");
            mockConfig.Setup(x => x.GetSection("ExternalApi:Port")).Returns(mockPortSection.Object);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Simulovaná chyba"));

            var httpClient = new HttpClient(handlerMock.Object);
            var service = new ExternalApiService(httpClient, mockConfig.Object, mockLogger.Object);

            await service.SendSellRecommendations(new List<StockTransaction> { new StockTransaction("AAPL", DateTime.Now) });

            mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Chyba při odesílání doporučení k prodeji"))), Times.Once);
        }


    }
}
