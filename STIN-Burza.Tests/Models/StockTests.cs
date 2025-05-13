using STIN_Burza.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STIN_Burza.Tests.Models
{
    public class StockTests
    {
        [Fact]
        public void AddPrice_ShouldAddPriceToHistory()
        {
            // Arrange
            var stock = new Stock("TestStock");
            var date = DateTime.Now;
            var price = 100.0;
            // Act
            stock.AddPrice(date, price);
            // Assert
            Assert.Single(stock.PriceHistory);
            Assert.Equal(date, stock.PriceHistory[0].Date);
            Assert.Equal(price, stock.PriceHistory[0].Price);
        }
        [Fact]
        public void AddPrice_ShouldAddMultiplePricesToHistory()
        {
            // Arrange
            var stock = new Stock("TestStock");
            var date1 = DateTime.Now;
            var price1 = 100.0;
            var date2 = DateTime.Now.AddDays(1);
            var price2 = 200.0;
            // Act
            stock.AddPrice(date1, price1);
            stock.AddPrice(date2, price2);
            // Assert
            Assert.Equal(2, stock.PriceHistory.Count);
            Assert.Equal(date1, stock.PriceHistory[0].Date);
            Assert.Equal(price1, stock.PriceHistory[0].Price);
            Assert.Equal(date2, stock.PriceHistory[1].Date);
            Assert.Equal(price2, stock.PriceHistory[1].Price);
        }
    }
}
