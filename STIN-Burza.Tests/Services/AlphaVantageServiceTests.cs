using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using STIN_Burza.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace STIN_Burza.Tests.Services
{
    public class AlphaVantageServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IMyLogger> _mockLogger;
        private readonly HttpClient _httpClient;
        private readonly AlphaVantageService _service;
        private readonly Mock<HttpMessageHandler> _mockMessageHandler;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

        public AlphaVantageServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<IMyLogger>();
            _mockMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _httpClient = new HttpClient(_mockMessageHandler.Object);
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);

            _mockConfig.Setup(config => config["AlphaVantage:ApiKey"]).Returns("test_api_key");
            _mockConfig.Setup(config => config["Configuration:WorkingDaysBackValues"]).Returns("7");

            _service = new AlphaVantageService(_mockConfig.Object, _mockLogger.Object, _mockHttpClientFactory.Object);
        }

        [Fact]
        public async Task GetStockWithHistoryAsync_ReturnsStock_WhenApiReturnsValidData()
        {
            // Arrange
            var symbol = "AAPL";
            var dailyJsonResponse = @"{
                    ""Meta Data"": {
                        ""1. Information"": ""Daily Prices (open, high, low, close) and Volumes"",
                        ""2. Symbol"": ""AAPL"",
                        ""3. Last Refreshed"": ""2025-05-14"",
                        ""4. Output Size"": ""Compact"",
                        ""5. Time Zone"": ""US/Eastern""
                    },
                    ""Time Series (Daily)"": {
                        ""2025-05-14"": { ""1. open"": ""150.00"", ""2. high"": ""155.00"", ""3. low"": ""149.00"", ""4. close"": ""150.00"", ""5. volume"": ""1000000"" },
                        ""2025-05-13"": { ""1. open"": ""148.00"", ""2. high"": ""151.00"", ""3. low"": ""147.00"", ""4. close"": ""148.00"", ""5. volume"": ""900000"" },
                        ""2025-05-12"": { ""1. open"": ""147.00"", ""2. high"": ""150.00"", ""3. low"": ""146.00"", ""4. close"": ""147.50"", ""5. volume"": ""800000"" },
                        ""2025-05-09"": { ""1. open"": ""146.00"", ""2. high"": ""148.00"", ""3. low"": ""145.00"", ""4. close"": ""146.50"", ""5. volume"": ""850000"" },
                        ""2025-05-08"": { ""1. open"": ""145.00"", ""2. high"": ""147.00"", ""3. low"": ""144.00"", ""4. close"": ""145.50"", ""5. volume"": ""870000"" },
                        ""2025-05-07"": { ""1. open"": ""144.00"", ""2. high"": ""146.00"", ""3. low"": ""143.00"", ""4. close"": ""144.50"", ""5. volume"": ""860000"" },
                        ""2025-05-06"": { ""1. open"": ""143.00"", ""2. high"": ""145.00"", ""3. low"": ""142.00"", ""4. close"": ""143.50"", ""5. volume"": ""880000"" }
                    }
                }";
            var intradayJsonResponse = @"{
                    ""Time Series (15min)"": {
                        ""2025-05-14 16:00:00"": { ""4. close"": ""150.00"" }
                    }
                }";

            _mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("TIME_SERIES_INTRADAY")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(intradayJsonResponse)
                });

            _mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("TIME_SERIES_DAILY")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(dailyJsonResponse)
                });

            // Act
            var result = await _service.GetStockWithHistoryAsync(symbol);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Name);
            Assert.NotNull(result.PriceHistory);
            Assert.True(result.PriceHistory.Count >= 7);
            Assert.Contains(result.PriceHistory, p => p.Date == new DateTime(2025, 5, 14) && p.Price == 150.00);
            Assert.Contains(result.PriceHistory, p => p.Date == new DateTime(2025, 5, 13) && p.Price == 148.00);
        }
    }
}
