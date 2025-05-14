using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using STIN_Burza.Controllers;
using STIN_Burza.Models;
using STIN_Burza.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace STIN_Burza.Tests.Controllers
{
    public class RatingApiControllerTests
    {
        private readonly Mock<IMyLogger> _mockLogger;
        private readonly Mock<IExternalApiService> _mockExternalApiService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly RatingApiController _controller;

        public RatingApiControllerTests()
        {
            _mockLogger = new Mock<IMyLogger>();
            _mockExternalApiService = new Mock<IExternalApiService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _controller = new RatingApiController(_mockLogger.Object, _mockExternalApiService.Object, _mockConfiguration.Object);

            // Nastavení výchozí hodnoty pro konfiguraci pomocí indexeru
            _mockConfiguration.Setup(config => config[It.Is<string>(s => s == "Configuration:RatingThreshold")]).Returns("0");
        }

        [Fact]
        public void ControllerConstructor_ConfigurationIsNotNull()
        {
            // Arrange
            var mockLogger = new Mock<IMyLogger>();
            var mockExternalApiService = new Mock<IExternalApiService>();
            var mockConfiguration = new Mock<IConfiguration>();

            // Act
            var controller = new RatingApiController(mockLogger.Object, mockExternalApiService.Object, mockConfiguration.Object);

            // Assert
            var configurationField = typeof(RatingApiController).GetField("_configuration", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(configurationField);

            var configurationValue = configurationField?.GetValue(controller);
            Assert.NotNull(configurationValue);
        }

        [Fact]
        public async Task ReceiveRating_InvalidJsonFormat_ReturnsBadRequest()
        {
            // Arrange
            var invalidJson = JsonDocument.Parse("{}").RootElement;

            // Act
            var result = await _controller.ReceiveRating(invalidJson);
            var badRequestResult = result as BadRequestObjectResult;

            // Assert
            Assert.NotNull(badRequestResult);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            Assert.Equal("Očekáváno JSON pole s hodnoceními.", badRequestResult.Value);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Přijatá data nejsou ve formátu JSON pole."))), Times.Once);
            _mockExternalApiService.Verify(service => service.SendSellRecommendations(It.IsAny<List<StockTransaction>>()), Times.Never);
        }

        [Fact]
        public async Task ReceiveRating_EmptyJsonArray_ReturnsOkNoRecommendations()
        {
            // Arrange
            var emptyJsonArray = JsonDocument.Parse("[]").RootElement;

            // Act
            var result = await _controller.ReceiveRating(emptyJsonArray);
            var okResult = result as OkObjectResult;

            // Assert
            Assert.NotNull(okResult);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Equal("Žádná platná hodnocení k odeslání doporučení.", okResult.Value);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Data jsou typu JSON."))), Times.Once);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Žádná platná hodnocení ke zpracování"))), Times.Once);
            _mockExternalApiService.Verify(service => service.SendSellRecommendations(It.IsAny<List<StockTransaction>>()), Times.Never);
        }

        [Fact]
        public async Task ReceiveRating_ValidTransactionsBelowThreshold_SendsRecommendations()
        {
            // Arrange
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(x => x.Value).Returns("0");
            _mockConfiguration.Setup(x => x.GetSection("Configuration:RatingThreshold")).Returns(mockSection.Object);

            var validJsonArray = JsonDocument.Parse(@"
            [
                { ""name"": ""AAPL"", ""date"": ""2025-05-14"", ""rating"": -1, ""sell"": 0 },
                { ""name"": ""GOOG"", ""date"": ""2025-05-14"", ""rating"": -5, ""sell"": 0 }
            ]").RootElement;

            // Act
            var result = await _controller.ReceiveRating(validJsonArray);
            var okResult = result as OkObjectResult;

            // Assert
            Assert.NotNull(okResult);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Equal("Hodnocení zpracována a doporučení odeslána.", okResult.Value);
            _mockExternalApiService.Verify(service => service.SendSellRecommendations(It.Is<List<StockTransaction>>(list =>
                list.Count == 2 &&
                list.All(t => t.Sell == 1) // Protože rating je pod prahem 0
            )), Times.Once);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Předávám k odeslání"))), Times.Once);
        }

        [Fact]
        public async Task ReceiveRating_ValidTransactionsAboveThreshold_SendsNoSellRecommendations()
        {
            // Arrange
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(x => x.Value).Returns("1");
            _mockConfiguration.Setup(x => x.GetSection("Configuration:RatingThreshold")).Returns(mockSection.Object);

            var validJsonArray = JsonDocument.Parse(@"
            [
                { ""name"": ""AAPL"", ""date"": ""2025-05-14"", ""rating"": 2, ""sell"": 0 },
                { ""name"": ""GOOG"", ""date"": ""2025-05-14"", ""rating"": 5, ""sell"": 0 }
            ]").RootElement;

            // Act
            var result = await _controller.ReceiveRating(validJsonArray);
            var okResult = result as OkObjectResult;

            // Assert
            Assert.NotNull(okResult);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Equal("Hodnocení zpracována a doporučení odeslána.", okResult.Value);
            _mockExternalApiService.Verify(service => service.SendSellRecommendations(It.Is<List<StockTransaction>>(list =>
                list.Count == 2 &&
                list.All(t => t.Sell == 0) // Protože rating je nad prahem 1
            )), Times.Once);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Předávám k odeslání"))), Times.Once);
        }

        [Fact]
        public async Task ReceiveRating_MixedValidAndInvalidTransactions_ProcessesValidAndLogsInvalid()
        {
            // Arrange
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(x => x.Value).Returns("0");
            _mockConfiguration.Setup(x => x.GetSection("Configuration:RatingThreshold")).Returns(mockSection.Object);

            var mixedJsonArray = JsonDocument.Parse(@"
    [
        { ""name"": ""AAPL"", ""date"": ""2025-05-14"", ""rating"": -1, ""sell"": 0 },
        ""not an object"",
        { ""name"": ""MSFT"", ""date"": ""2025-05-14"", ""rating"": 3, ""sell"": 0 },
        123,
        true,
        null,
        { ""name"": ""NVDA"", ""date"": ""2025-05-14"" },
        { ""name"": """", ""date"": ""2025-05-14"", ""rating"": 3, ""sell"": 0 }
    ]").RootElement;

            // Act
            var result = await _controller.ReceiveRating(mixedJsonArray);
            var okResult = result as OkObjectResult;

            // Assert
            Assert.NotNull(okResult);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Equal("Hodnocení zpracována a doporučení odeslána.", okResult.Value);
            _mockExternalApiService.Verify(service => service.SendSellRecommendations(It.Is<List<StockTransaction>>(list =>
                list.Count == 2 &&
                list.Any(t => t.Name == "AAPL" && t.Sell == 1) &&
                list.Any(t => t.Name == "MSFT" && t.Sell == 0)
            )), Times.Once);
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Přijatý prvek pole není JSON objekt"))), Times.Exactly(4));
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Přijata položka s neplatnými daty"))), Times.Exactly(1));
            _mockLogger.Verify(logger => logger.Log(It.Is<string>(s => s.Contains("Předávám k odeslání"))), Times.Once);
        }
    }
}