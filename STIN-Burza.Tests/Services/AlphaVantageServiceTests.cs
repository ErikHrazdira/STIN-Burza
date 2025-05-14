using Moq;
using STIN_Burza.Models;
using STIN_Burza.Services;
using Microsoft.Extensions.Configuration;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace STIN_Burza.Tests.Services
{
    public class AlphaVantageServiceTests
    {
        [Fact]
        public async Task GetStockWithHistoryAsync_ReturnsStockWithPrices_WhenProviderReturnsData()
        {
            // Arrange
            var mockLogger = new Mock<IMyLogger>();
            var mockProvider = new Mock<IAlphaVantageDataProvider>();
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Configuration:WorkingDaysBackValues"]).Returns("3");

            mockProvider.Setup(p => p.GetIntradayPriceAsync("AAPL"))
                .ReturnsAsync(150.0);
            mockProvider.Setup(p => p.GetDailyPricesAsync("AAPL", It.IsAny<int>()))
                .ReturnsAsync(new List<StockPrice>
                {
                new StockPrice(DateTime.Today.AddDays(-1), 148.0),
                new StockPrice(DateTime.Today.AddDays(-2), 147.0)
                });

            var service = new AlphaVantageService(mockConfig.Object, mockLogger.Object, mockProvider.Object);

            // Act
            var result = await service.GetStockWithHistoryAsync("AAPL");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("AAPL", result.Name);
            Assert.Equal(3, result.PriceHistory.Count);
            Assert.Contains(result.PriceHistory, p => p.Price == 150.0);
            Assert.Contains(result.PriceHistory, p => p.Price == 148.0);
            Assert.Contains(result.PriceHistory, p => p.Price == 147.0);
            mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Stažena data pro symbol"))), Times.Once);
        }

        [Fact]
        public async Task GetStockWithHistoryAsync_ReturnsStockWithoutTodayPrice_OnWeekend()
        {
            // Arrange
            var mockLogger = new Mock<IMyLogger>();
            var mockProvider = new Mock<IAlphaVantageDataProvider>();
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Configuration:WorkingDaysBackValues"]).Returns("2");

            // Připrav data pro test
            mockProvider.Setup(p => p.GetDailyPricesAsync("AAPL", It.IsAny<int>()))
                .ReturnsAsync(new List<StockPrice>
                {
            new StockPrice(DateTime.Today.AddDays(-1), 148.0),
            new StockPrice(DateTime.Today.AddDays(-2), 147.0)
                });

            var service = new AlphaVantageService(mockConfig.Object, mockLogger.Object, mockProvider.Object);

            // Act
            var result = await service.GetStockWithHistoryAsync("AAPL");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("AAPL", result.Name);
            Assert.Equal(2, result.PriceHistory.Count);
        }

        [Fact]
        public async Task GetStockWithHistoryAsync_ReturnsNull_AndLogs_OnException()
        {
            // Arrange
            var mockLogger = new Mock<IMyLogger>();
            var mockProvider = new Mock<IAlphaVantageDataProvider>();
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Configuration:WorkingDaysBackValues"]).Returns("2");

            mockProvider.Setup(p => p.GetIntradayPriceAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("API error"));

            var service = new AlphaVantageService(mockConfig.Object, mockLogger.Object, mockProvider.Object);

            // Act
            var result = await service.GetStockWithHistoryAsync("AAPL");

            // Assert
            Assert.Null(result);
            mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Chyba při stahování dat"))), Times.Once);
        }

    }
}