using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using STIN_Burza.Models;
using STIN_Burza.Services;
using Xunit;

namespace STIN_Burza.Tests.Services
{
    public class AlphaVantageDataProviderTests
    {
        private static AlphaVantageDataProvider CreateProvider(string responseJson, string expectedUrlPart, Mock<ILogger<AlphaVantageDataProvider>>? mockLogger = null)
        {
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["AlphaVantage:ApiKey"]).Returns("testkey");

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains(expectedUrlPart)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });
            handlerMock.Protected()
                .Setup("Dispose", ItExpr.IsAny<bool>());

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(handlerMock.Object);
            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var logger = mockLogger?.Object ?? new Mock<ILogger<AlphaVantageDataProvider>>().Object;

            return new AlphaVantageDataProvider(mockConfig.Object, logger, httpClientFactoryMock.Object);
        }

        [Fact]
        public async Task GetIntradayPriceAsync_ReturnsPrice_WhenValidJson()
        {
            // Arrange
            var json = @"{
                    ""Time Series (15min)"": {
                        ""2025-05-14 16:00:00"": { ""4. close"": ""123.45"" }
                    }
                }";
            var provider = CreateProvider(json, "TIME_SERIES_INTRADAY");

            // Act
            var price = await provider.GetIntradayPriceAsync("AAPL");

            // Assert
            Assert.NotNull(price);
            Assert.Equal(123.45, price);
        }

        [Fact]
        public async Task GetDailyPricesAsync_ReturnsList_WhenValidJson()
        {
            // Arrange
            var json = @"{
                    ""Time Series (Daily)"": {
                        ""2025-05-14"": { ""4. close"": ""150.00"" },
                        ""2025-05-13"": { ""4. close"": ""148.00"" }
                    }
                }";
            var provider = CreateProvider(json, "TIME_SERIES_DAILY");

            // Act
            var prices = await provider.GetDailyPricesAsync("AAPL", 2);

            // Assert
            Assert.NotNull(prices);
            Assert.Equal(2, prices.Count);
            Assert.Contains(prices, p => p.Date == new DateTime(2025, 5, 14) && p.Price == 150.00);
            Assert.Contains(prices, p => p.Date == new DateTime(2025, 5, 13) && p.Price == 148.00);
        }

        [Fact]
        public async Task GetIntradayPriceAsync_LogsWarningAndReturnsNull_WhenNoTimeSeries()
        {
            // Arrange
            var json = @"{ ""Meta Data"": {} }"; // Chybí "Time Series (15min)"
            var mockLogger = new Mock<ILogger<AlphaVantageDataProvider>>();
            var provider = CreateProvider(json, "TIME_SERIES_INTRADAY", mockLogger);

            // Act
            var price = await provider.GetIntradayPriceAsync("AAPL");

            // Assert
            Assert.Null(price);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Could not retrieve today's intraday price")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once);
        }

        [Fact]
        public async Task GetIntradayPriceAsync_LogsErrorAndReturnsNull_OnException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AlphaVantageDataProvider>>();
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["AlphaVantage:ApiKey"]).Returns("testkey");

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception("Simulovaná chyba"));

            handlerMock.Protected().Setup("Dispose", ItExpr.IsAny<bool>());

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(handlerMock.Object);
            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var provider = new AlphaVantageDataProvider(mockConfig.Object, mockLogger.Object, httpClientFactoryMock.Object);

            // Act
            var price = await provider.GetIntradayPriceAsync("AAPL");

            // Assert
            Assert.Null(price);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error retrieving intraday price")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once);
        }

        [Fact]
        public async Task GetDailyPricesAsync_LogsErrorAndReturnsEmptyList_OnException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AlphaVantageDataProvider>>();
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["AlphaVantage:ApiKey"]).Returns("testkey");

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception("Simulovaná chyba"));

            handlerMock.Protected().Setup("Dispose", ItExpr.IsAny<bool>());

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(handlerMock.Object);
            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var provider = new AlphaVantageDataProvider(mockConfig.Object, mockLogger.Object, httpClientFactoryMock.Object);

            // Act
            var prices = await provider.GetDailyPricesAsync("AAPL", 2);

            // Assert
            Assert.NotNull(prices);
            Assert.Empty(prices);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error retrieving daily prices")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once);
        }

        [Fact]
        public void Constructor_ThrowsException_WhenApiKeyMissing()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["AlphaVantage:ApiKey"]).Returns((string?)null);
            var logger = new Mock<ILogger<AlphaVantageDataProvider>>();
            var httpClientFactory = new Mock<IHttpClientFactory>();

            // Act & Assert
            var ex = Assert.Throws<Exception>(() =>
                new AlphaVantageDataProvider(mockConfig.Object, logger.Object, httpClientFactory.Object));
            Assert.Contains("API klíč není nastaven", ex.Message);
        }

        [Fact]
        public async Task GetDailyPricesAsync_LogsWarningAndReturnsEmptyList_WhenNoTimeSeries()
        {
            // Arrange
            var json = @"{ ""Meta Data"": {} }"; // Chybí "Time Series (Daily)"
            var mockLogger = new Mock<ILogger<AlphaVantageDataProvider>>();
            var provider = CreateProvider(json, "TIME_SERIES_DAILY", mockLogger);

            // Act
            var prices = await provider.GetDailyPricesAsync("AAPL", 2);

            // Assert
            Assert.NotNull(prices);
            Assert.Empty(prices);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Missing 'Time Series (Daily)' data")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once);
        }


        [Fact]
        public async Task GetDailyPricesAsync_LogsWarningAndSkipsInvalidPrice()
        {
            // Arrange
            var json = @"{
        ""Time Series (Daily)"": {
            ""2025-05-14"": { ""4. close"": ""not-a-number"" },
            ""2025-05-13"": { ""4. close"": ""148.00"" }
        }
    }";
            var mockLogger = new Mock<ILogger<AlphaVantageDataProvider>>();
            var provider = CreateProvider(json, "TIME_SERIES_DAILY", mockLogger);

            // Act
            var prices = await provider.GetDailyPricesAsync("AAPL", 2);

            // Assert
            Assert.Single(prices);
            Assert.Equal(new DateTime(2025, 5, 13), prices[0].Date);
            Assert.Equal(148.00, prices[0].Price);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Could not parse price for date")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once);
        }


    }
}
