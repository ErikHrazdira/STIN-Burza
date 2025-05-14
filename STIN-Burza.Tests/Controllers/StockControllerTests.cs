using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;
using STIN_Burza.Controllers;
using STIN_Burza.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STIN_Burza.Models;
using Microsoft.AspNetCore.Mvc;

namespace STIN_Burza.Tests.Controllers
{
    public class StockControllerTests
    {
        private readonly Mock<IStockService> _mockStockService;
        private readonly Mock<IMyLogger> _mockLogger;
        private readonly Mock<IAlphaVantageService> _mockAlphaVantageService;
        private readonly Mock<IStockFilterManager> _mockStockFilterManager;
        private readonly Mock<IExternalApiService> _mockExternalApiService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly StockController _controller;

        public StockControllerTests()
        {
            _mockStockService = new Mock<IStockService>();
            _mockLogger = new Mock<IMyLogger>();
            _mockAlphaVantageService = new Mock<IAlphaVantageService>();
            _mockStockFilterManager = new Mock<IStockFilterManager>();
            _mockExternalApiService = new Mock<IExternalApiService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();

            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(x => x.Value).Returns("7");
            _mockConfiguration.Setup(x => x.GetSection("Configuration:WorkingDaysBackValues")).Returns(mockSection.Object);
            _mockConfiguration.SetupGet(config => config["Configuration:RatingThresholdFilePath"]).Returns("test_path.txt");
            _mockEnvironment.Setup(e => e.ContentRootPath).Returns("test_root_path");

            _controller = new StockController(
                _mockStockService.Object,
                _mockLogger.Object,
                _mockAlphaVantageService.Object,
                _mockStockFilterManager.Object,
                _mockExternalApiService.Object,
                _mockConfiguration.Object,
                _mockEnvironment.Object
            );
        }

        [Fact]
        public void Index_ReturnsViewWithFavoriteStocks()
        {
            // Arrange
            var favoriteStocks = new List<Stock> { new Stock("AAPL"), new Stock("GOOG") };
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(favoriteStocks);
            var logLines = new List<string> { "Log line 1", "Log line 2" };
            _mockLogger.Setup(logger => logger.GetLastLines(It.IsAny<int>())).Returns(logLines);

            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(favoriteStocks, result.Model);
            Assert.Equal(logLines, result.ViewData["LogLines"]);
        }

        [Fact]
        public void Index_ReturnsEmptyListIfNoFavoriteStocks()
        {
            // Arrange
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(new List<Stock>());
            var logLines = new List<string> { "Another log line" };
            _mockLogger.Setup(logger => logger.GetLastLines(It.IsAny<int>())).Returns(logLines);

            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = result.Model as List<Stock>;
            Assert.NotNull(model); // Ensure the model is not null
            Assert.Empty(model);
            Assert.Equal(logLines, result.ViewData["LogLines"]);
        }

        [Fact]
        public void Index_LoadsLogLinesForViewBag()
        {
            // Arrange
            var logLines = new List<string> { "Test log" };
            _mockLogger.Setup(logger => logger.GetLastLines(It.IsAny<int>())).Returns(logLines);
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(new List<Stock>());

            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(logLines, result.ViewData["LogLines"]);
        }

        [Fact]
        public void RemoveFavorite_ExistingStock_RemovesAndRedirectsToIndex()
        {
            // Arrange
            var favoriteStocks = new List<Stock> { new Stock("AAPL"), new Stock("GOOG") };
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(favoriteStocks);
            _mockStockService.Setup(service => service.SaveFavoriteStocks(It.Is<List<Stock>>(list => list.Count == 1 && list.Any(s => s.Name == "GOOG"))));

            // Act
            var result = _controller.RemoveFavorite("AAPL") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockStockService.Verify(service => service.LoadFavoriteStocks(), Times.Once);
            _mockStockService.Verify(service => service.SaveFavoriteStocks(It.Is<List<Stock>>(list => list.Count == 1 && list.Any(s => s.Name == "GOOG"))), Times.Once);
        }

        [Fact]
        public void RemoveFavorite_NonExistingStock_LogsAndRedirectsToIndex()
        {
            // Arrange
            var favoriteStocks = new List<Stock> { new Stock("MSFT"), new Stock("NVDA") };
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(favoriteStocks);
            _mockStockService.Verify(service => service.SaveFavoriteStocks(It.IsAny<List<Stock>>()), Times.Never);

            // Act
            var result = _controller.RemoveFavorite("AAPL") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockStockService.Verify(service => service.LoadFavoriteStocks(), Times.Once);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("neexistuje"))), Times.Once);
        }

        [Fact]
        public void RemoveFavorite_CaseInsensitiveNameMatch()
        {
            // Arrange
            var favoriteStocks = new List<Stock> { new Stock("aapl") };
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(favoriteStocks);
            _mockStockService.Setup(service => service.SaveFavoriteStocks(It.Is<List<Stock>>(list => list.Count == 0)));

            // Act
            var result = _controller.RemoveFavorite("AAPL") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockStockService.Verify(service => service.LoadFavoriteStocks(), Times.Once);
            _mockStockService.Verify(service => service.SaveFavoriteStocks(It.IsAny<List<Stock>>()), Times.Once);
        }

        [Fact]
        public async Task AddFavorite_ExistingStock_LogsAndRedirectsToIndex()
        {
            // Arrange
            var favoriteStocks = new List<Stock> { new Stock("AAPL"), new Stock("GOOG") };
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(favoriteStocks);

            // Act
            var result = await _controller.AddFavorite("AAPL") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockStockService.Verify(service => service.LoadFavoriteStocks(), Times.Once);
            _mockAlphaVantageService.Verify(service => service.GetStockWithHistoryAsync(It.IsAny<string>()), Times.Never);
            _mockStockService.Verify(service => service.SaveFavoriteStocks(It.IsAny<List<Stock>>()), Times.Never);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("je v oblíbených."))), Times.Once);
        }

        [Fact]
        public async Task AddFavorite_NewStock_AddsAndRedirectsToIndex()
        {
            // Arrange
            var favoriteStocks = new List<Stock> { new Stock("GOOG") };
            var newStock = new Stock("AAPL") { PriceHistory = new List<StockPrice> { new StockPrice(DateTime.Now, 100.0) } };
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(favoriteStocks);
            _mockAlphaVantageService.Setup(service => service.GetStockWithHistoryAsync("AAPL")).ReturnsAsync(newStock);
            _mockStockService.Setup(service => service.SaveFavoriteStocks(It.Is<List<Stock>>(list => list.Count == 2 && list.Any(s => s.Name == "AAPL"))));

            // Act
            var result = await _controller.AddFavorite("AAPL") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockStockService.Verify(service => service.LoadFavoriteStocks(), Times.Once);
            _mockAlphaVantageService.Verify(service => service.GetStockWithHistoryAsync("AAPL"), Times.Once);
            _mockStockService.Verify(service => service.SaveFavoriteStocks(It.IsAny<List<Stock>>()), Times.Once);
        }

        [Fact]
        public async Task AddFavorite_NewStockNotFound_LogsAndRedirectsToIndex()
        {
            // Arrange
            var favoriteStocks = new List<Stock> { new Stock("GOOG") };
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(favoriteStocks);
            _mockAlphaVantageService.Setup(service => service.GetStockWithHistoryAsync("AAPL")).ReturnsAsync((Stock?)null);

            // Act
            var result = await _controller.AddFavorite("AAPL") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockStockService.Verify(service => service.LoadFavoriteStocks(), Times.Once);
            _mockAlphaVantageService.Verify(service => service.GetStockWithHistoryAsync("AAPL"), Times.Once);
            _mockStockService.Verify(service => service.SaveFavoriteStocks(It.IsAny<List<Stock>>()), Times.Never);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("nenalezena"))), Times.Once);
        }

        [Fact]
        public async Task AddFavorite_NewStockWithoutHistory_LogsAndRedirectsToIndex()
        {
            // Arrange
            var favoriteStocks = new List<Stock> { new Stock("GOOG") };
            var newStock = new Stock("AAPL") { PriceHistory = new List<StockPrice>() }; // Prázdná historie
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(favoriteStocks);
            _mockAlphaVantageService.Setup(service => service.GetStockWithHistoryAsync("AAPL")).ReturnsAsync(newStock);

            // Act
            var result = await _controller.AddFavorite("AAPL") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockStockService.Verify(service => service.LoadFavoriteStocks(), Times.Once);
            _mockAlphaVantageService.Verify(service => service.GetStockWithHistoryAsync("AAPL"), Times.Once);
            _mockStockService.Verify(service => service.SaveFavoriteStocks(It.IsAny<List<Stock>>()), Times.Never);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("nenalezena"))), Times.Once);
        }

        [Fact]
        public async Task AddFavorite_CaseInsensitiveDuplicateCheck()
        {
            // Arrange
            var favoriteStocks = new List<Stock> { new Stock("aapl") };
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(favoriteStocks);

            // Act
            var result = await _controller.AddFavorite("AAPL") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockStockService.Verify(service => service.LoadFavoriteStocks(), Times.Once);
            _mockAlphaVantageService.Verify(service => service.GetStockWithHistoryAsync(It.IsAny<string>()), Times.Never);
            _mockStockService.Verify(service => service.SaveFavoriteStocks(It.IsAny<List<Stock>>()), Times.Never);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("je v oblíbených"))), Times.Once);
        }

        [Fact]
        public async Task UpdateAllFavorites_NoFavorites_RedirectsToIndex()
        {
            // Arrange
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(new List<Stock>());

            // Act
            var result = await _controller.UpdateAllFavorites() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockStockService.Verify(service => service.LoadFavoriteStocks(), Times.Once);
            _mockAlphaVantageService.Verify(service => service.GetStockWithHistoryAsync(It.IsAny<string>()), Times.Never);
            _mockStockService.Verify(service => service.SaveFavoriteStocks(It.IsAny<List<Stock>>()), Times.Once);
        }


        [Fact]
        public async Task UpdateAllFavorites_UpdateSuccessful()
        {
            // Arrange
            var favoriteStocks = new List<Stock>
            {
                new Stock("AAPL")
                {
                    PriceHistory = new List<StockPrice>
                    {
                        new StockPrice(DateTime.Now.Date.AddDays(-10), 140)
                    }
                }
            };
            var updatedStock = new Stock("AAPL") { PriceHistory = new List<StockPrice> { new StockPrice(DateTime.Now.Date, 155) } };
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(favoriteStocks);
            _mockAlphaVantageService.Setup(service => service.GetStockWithHistoryAsync("AAPL")).ReturnsAsync(updatedStock);

            // Act
            var result = await _controller.UpdateAllFavorites() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockStockService.Verify(service => service.LoadFavoriteStocks(), Times.Once);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Aktualizuji historická data"))), Times.Once);
            _mockAlphaVantageService.Verify(service => service.GetStockWithHistoryAsync("AAPL"), Times.Once);
            _mockStockService.Verify(service => service.SaveFavoriteStocks(It.Is<List<Stock>>(list =>
                list.First().PriceHistory.Any(p => p.Date.Date == DateTime.Now.Date && p.Price == 155))), Times.Once);
        }

        [Fact]
        public async Task UpdateAllFavorites_UpdateFails()
        {
            // Arrange
            var favoriteStocks = new List<Stock>
            {
                new Stock("AAPL")
                {
                    PriceHistory = new List<StockPrice>
                    {
                        new StockPrice(DateTime.Now.Date.AddDays(-10), 140)
                    }
                }
            };
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(favoriteStocks);
            _mockAlphaVantageService.Setup(service => service.GetStockWithHistoryAsync("AAPL")).ReturnsAsync((Stock?)null);

            // Act
            var result = await _controller.UpdateAllFavorites() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockStockService.Verify(service => service.LoadFavoriteStocks(), Times.Once);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Aktualizuji historická data"))), Times.Once);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Nepodařilo se získat aktualizovaná data"))), Times.Once);
            _mockAlphaVantageService.Verify(service => service.GetStockWithHistoryAsync("AAPL"), Times.Once);
            _mockStockService.Verify(service => service.SaveFavoriteStocks(It.Is<List<Stock>>(list => list.SequenceEqual(favoriteStocks))), Times.Once);
        }

        [Fact]
        public async Task StartProcess_NoFavorites_LogsAndRedirectsToIndex()
        {
            // Arrange
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(new List<Stock>());
            _mockStockFilterManager.Setup(manager => manager.GetPassingStockNames(It.IsAny<List<Stock>>())).Returns(new List<string>());

            // Act
            var result = await _controller.StartProcess() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Spuštěn proces"))), Times.Once);
            _mockStockService.Verify(service => service.LoadFavoriteStocks(), Times.Once);
            _mockStockFilterManager.Verify(manager => manager.GetPassingStockNames(It.IsAny<List<Stock>>()), Times.Once);
            _mockExternalApiService.Verify(service => service.SendPassingStockNames(It.IsAny<List<string>>()), Times.Never);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Žádná položka neprošla"))), Times.Once);
        }

        [Fact]
        public async Task StartProcess_NoPassingStocks_LogsAndRedirectsToIndex()
        {
            // Arrange
            var favoriteStocks = new List<Stock> { new Stock("AAPL") { PriceHistory = new List<StockPrice>() } };
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(favoriteStocks);
            _mockStockFilterManager.Setup(manager => manager.GetPassingStockNames(favoriteStocks)).Returns(new List<string>());

            // Act
            var result = await _controller.StartProcess() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Spuštěn proces"))), Times.Once);
            _mockStockService.Verify(service => service.LoadFavoriteStocks(), Times.Once);
            _mockStockFilterManager.Verify(manager => manager.GetPassingStockNames(favoriteStocks), Times.Once);
            _mockExternalApiService.Verify(service => service.SendPassingStockNames(It.IsAny<List<string>>()), Times.Never);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Žádná položka neprošla"))), Times.Once);
        }

        [Fact]
        public async Task StartProcess_PassingStocks_LogsAndSendsDataToExternalApi()
        {
            // Arrange
            var favoriteStocks = new List<Stock>
            {
                new Stock("AAPL") { PriceHistory = new List<StockPrice>() },
                new Stock("GOOG") { PriceHistory = new List<StockPrice>() }
            };
            _mockStockService.Setup(service => service.LoadFavoriteStocks()).Returns(favoriteStocks);
            var passingStockNames = new List<string> { "AAPL" };
            _mockStockFilterManager.Setup(manager => manager.GetPassingStockNames(favoriteStocks)).Returns(passingStockNames);

            // Act
            var result = await _controller.StartProcess() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Spuštěn proces"))), Times.Once);
            _mockStockService.Verify(service => service.LoadFavoriteStocks(), Times.Once);
            _mockStockFilterManager.Verify(manager => manager.GetPassingStockNames(favoriteStocks), Times.Once);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains($"Odesílám informace o položkách: {string.Join(", ", passingStockNames)}"))), Times.Once);
            _mockExternalApiService.Verify(service => service.SendPassingStockNames(passingStockNames), Times.Once);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Odesílání na externí API dokončeno"))), Times.Once);
        }


        [Fact]
        public void UpdateRatingThreshold_ValidInput_SavesThresholdAndRedirectsToIndex()
        {
            // Arrange
            int testThreshold = 5;

            // Act
            var result = _controller.UpdateRatingThreshold(testThreshold) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains($"Uživatel uložil nový práh hodnocení: {testThreshold}"))), Times.Once);
        }

        [Fact]
        public void UpdateRatingThreshold_ValidInput_LogsInformationAboutSave()
        {
            // Arrange
            int testThreshold = 0;

            // Act
            var result = _controller.UpdateRatingThreshold(testThreshold) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("uložil nový práh"))), Times.Once);
        }
        [Fact]
        public void UpdateRatingThreshold_InvalidInputTooHigh_LogsErrorAndRedirectsToIndex_SimpleLogCheck()
        {
            // Arrange
            int testThreshold = 11;

            // Act
            var result = _controller.UpdateRatingThreshold(testThreshold) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("neplatný práh"))), Times.Once);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("uložil nový práh"))), Times.Never);
        }

        [Fact]
        public void UpdateRatingThreshold_InvalidInputTooLow_LogsErrorAndRedirectsToIndex_SimpleLogCheck()
        {
            // Arrange
            int testThreshold = -11;

            // Act
            var result = _controller.UpdateRatingThreshold(testThreshold) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("neplatný práh"))), Times.Once);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("uložil nový práh"))), Times.Never);
        }

        [Fact]
        public void ShouldUpdate_NoHistory_ReturnsTrue()
        {
            var history = new List<StockPrice>();
            int daysToCheck = 5;
            Assert.True(StockController.ShouldUpdate(history, daysToCheck));
        }

        [Fact]
        public void ShouldUpdate_HistoryWithTodayAndEnoughPreviousDays_ReturnsFalse()
        {
            var history = new List<StockPrice>();
            var today = DateTime.Now.Date;
            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(-i);
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    history.Add(new StockPrice(date, 100 - i));
                }
            }
            int daysToCheck = 5;
            Assert.False(StockController.ShouldUpdate(history.OrderByDescending(p => p.Date).ToList(), daysToCheck));
        }
    }
}
