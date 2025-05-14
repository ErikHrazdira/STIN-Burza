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

            // Nastavení pro konstruktor (simulace cesty k souboru pomocí SetupGet)
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
    }
}

