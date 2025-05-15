using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STIN_Burza.Models;
using STIN_Burza.Services;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Xunit;

namespace STIN_Burza.Tests.Services
{
    public class StockServiceTests
    {
        [Fact]
        public void LoadFavoriteStocks_ReturnsEmptyList_WhenFileDoesNotExist()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            var service = new StockServiceForTest(tempFile);

            // Act
            var result = service.LoadFavoriteStocks();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void SaveFavoriteStocks_And_LoadFavoriteStocks_WorksCorrectly()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            var service = new StockServiceForTest(tempFile);
            var stocks = new List<Stock>
        {
            new Stock("AAPL"),
            new Stock("GOOG")
        };

            // Act
            service.SaveFavoriteStocks(stocks);
            var loaded = service.LoadFavoriteStocks();

            // Assert
            Assert.Equal(2, loaded.Count);
            Assert.Contains(loaded, s => s.Name == "AAPL");
            Assert.Contains(loaded, s => s.Name == "GOOG");

            // Cleanup
            File.Delete(tempFile);
        }

        private class StockServiceForTest : StockService
        {
            private readonly string _testFilePath;
            public StockServiceForTest(string filePath) => _testFilePath = filePath;
            protected override string filePath => _testFilePath;
        }
    }
}
