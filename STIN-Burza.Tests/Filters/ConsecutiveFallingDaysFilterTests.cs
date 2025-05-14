using Microsoft.Extensions.Configuration;
using STIN_Burza.Filters;
using STIN_Burza.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STIN_Burza.Tests.Filters
{
    public class ConsecutiveFallingDaysFilterTests
    {
        private static IConfiguration GetConfig(int threshold)
        {
            var dict = new Dictionary<string, string>
            {
                ["StockFilters:ConsecutiveFallingDays:Threshold"] = threshold.ToString()
            };
            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        [Fact]
        public void ShouldFilterOut_ReturnsFalse_WhenNotEnoughHistory()
        {
            var config = GetConfig(3);
            var filter = new ConsecutiveFallingDaysFilter(config);
            var stock = new Stock("AAPL") { PriceHistory = new List<StockPrice> { new(DateTime.Today, 100) } };

            Assert.False(filter.ShouldFilterOut(stock));
        }

        [Fact]
        public void ShouldFilterOut_ReturnsFalse_WhenNoConsecutiveFalling()
        {
            var config = GetConfig(3);
            var filter = new ConsecutiveFallingDaysFilter(config);
            var stock = new Stock("AAPL")
            {
                PriceHistory = new List<StockPrice>
            {
                new(DateTime.Today, 100),
                new(DateTime.Today.AddDays(-1), 110),
                new(DateTime.Today.AddDays(-2), 120)
            }
            };

            Assert.False(filter.ShouldFilterOut(stock));
        }

        [Fact]
        public void ShouldFilterOut_ReturnsTrue_WhenConsecutiveFalling()
        {
            var config = GetConfig(3);
            var filter = new ConsecutiveFallingDaysFilter(config);
            var stock = new Stock("AAPL")
            {
                PriceHistory = new List<StockPrice>
            {
                new(DateTime.Today, 90),
                new(DateTime.Today.AddDays(-1), 100),
                new(DateTime.Today.AddDays(-2), 110),
                new(DateTime.Today.AddDays(-3), 120)
            }
            };

            Assert.True(filter.ShouldFilterOut(stock));
        }

        [Fact]
        public void ShouldFilterOut_UsesCustomThreshold()
        {
            var config = GetConfig(2);
            var filter = new ConsecutiveFallingDaysFilter(config);
            var stock = new Stock("AAPL")
            {
                PriceHistory = new List<StockPrice>
            {
                new(DateTime.Today, 80),
                new(DateTime.Today.AddDays(-1), 90),
                new(DateTime.Today.AddDays(-2), 100)
            }
            };

            Assert.True(filter.ShouldFilterOut(stock));
        }
    }
}
