using System.Collections.Generic;
using Moq;
using Xunit;
using STIN_Burza.Services;
using STIN_Burza.Models;
using STIN_Burza.Filters;

namespace STIN_Burza.Tests.Services
{
    public class StockFilterManagerTests
    {
        [Fact]
        public void GetPassingStockNames_ReturnsAll_WhenNoFilters()
        {
            // Arrange
            var logger = new Mock<IMyLogger>();
            var stocks = new List<Stock> { new("AAPL"), new("GOOG") };
            var manager = new StockFilterManager(new List<IStockFilter>(), logger.Object);

            // Act
            var result = manager.GetPassingStockNames(stocks);

            // Assert
            Assert.Equal(new[] { "AAPL", "GOOG" }, result);
        }

        [Fact]
        public void GetPassingStockNames_FiltersOutStocks()
        {
            // Arrange
            var logger = new Mock<IMyLogger>();
            var filter = new Mock<IStockFilter>();
            filter.Setup(f => f.ShouldFilterOut(It.Is<Stock>(s => s.Name == "AAPL"))).Returns(true);
            filter.Setup(f => f.ShouldFilterOut(It.Is<Stock>(s => s.Name == "GOOG"))).Returns(false);
            var stocks = new List<Stock> { new("AAPL"), new("GOOG") };
            var manager = new StockFilterManager(new[] { filter.Object }, logger.Object);

            // Act
            var result = manager.GetPassingStockNames(stocks);

            // Assert
            Assert.Single(result);
            Assert.Contains("GOOG", result);
            logger.Verify(l => l.Log(It.Is<string>(msg => msg.Contains("AAPL"))), Times.Once);
        }

        [Fact]
        public void GetPassingStockNames_StopsAtFirstFilter()
        {
            // Arrange
            var logger = new Mock<IMyLogger>();
            var filter1 = new Mock<IStockFilter>();
            var filter2 = new Mock<IStockFilter>();
            filter1.Setup(f => f.ShouldFilterOut(It.IsAny<Stock>())).Returns(true);
            filter2.Setup(f => f.ShouldFilterOut(It.IsAny<Stock>())).Returns(false);
            var stocks = new List<Stock> { new("AAPL") };
            var manager = new StockFilterManager(new[] { filter1.Object, filter2.Object }, logger.Object);

            // Act
            var result = manager.GetPassingStockNames(stocks);

            // Assert
            Assert.Empty(result);
            filter2.Verify(f => f.ShouldFilterOut(It.IsAny<Stock>()), Times.Never);
        }
    }
}
