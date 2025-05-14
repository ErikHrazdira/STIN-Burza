using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using STIN_Burza.Filters;
using STIN_Burza.Models;
using Xunit;

namespace STIN_Burza.Tests.Filters
{
    public class PriceDropsInLastWindowFilterTests
    {
        private static IConfiguration GetConfig(int dropCount, int lookback)
        {
            var dict = new Dictionary<string, string>
            {
                ["StockFilters:PriceDropsInWindow:DropCountThreshold"] = dropCount.ToString(),
                ["StockFilters:PriceDropsInWindow:LookbackDays"] = lookback.ToString()
            };
            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        [Fact]
        public void ShouldFilterOut_ReturnsFalse_WhenNotEnoughHistory()
        {
            var config = GetConfig(3, 5);
            var filter = new PriceDropsInLastWindowFilter(config);
            var stock = new Stock("AAPL") { PriceHistory = new List<StockPrice> { new(DateTime.Today, 100) } };

            Assert.False(filter.ShouldFilterOut(stock));
        }

        [Fact]
        public void ShouldFilterOut_ReturnsFalse_WhenNotEnoughDrops()
        {
            var config = GetConfig(2, 3);
            var filter = new PriceDropsInLastWindowFilter(config);
            var stock = new Stock("AAPL")
            {
                PriceHistory = new List<StockPrice>
            {
                new(DateTime.Today, 100),
                new(DateTime.Today.AddDays(-1), 110),
                new(DateTime.Today.AddDays(-2), 120)
            }
            };

            // 100 < 110 (drop), 110 < 120 (drop) => 2 drops, threshold 2 => should filter out
            Assert.True(filter.ShouldFilterOut(stock));

            // Change threshold to 3, should not filter out
            filter = new PriceDropsInLastWindowFilter(GetConfig(3, 3));
            Assert.False(filter.ShouldFilterOut(stock));
        }

        [Fact]
        public void ShouldFilterOut_UsesLookbackWindow()
        {
            var config = GetConfig(2, 2);
            var filter = new PriceDropsInLastWindowFilter(config);
            var stock = new Stock("AAPL")
            {
                PriceHistory = new List<StockPrice>
            {
                new(DateTime.Today, 90),
                new(DateTime.Today.AddDays(-1), 100),
                new(DateTime.Today.AddDays(-2), 110)
            }
            };

            // Only last 2 days: 90 < 100 (drop) => 1 drop, threshold 2 => should not filter out
            Assert.False(filter.ShouldFilterOut(stock));
        }

        [Fact]
        public void ShouldFilterOut_ReturnsTrue_WhenEnoughDropsInWindow()
        {
            var config = GetConfig(2, 3);
            var filter = new PriceDropsInLastWindowFilter(config);
            var stock = new Stock("AAPL")
            {
                PriceHistory = new List<StockPrice>
            {
                new(DateTime.Today, 80),
                new(DateTime.Today.AddDays(-1), 90),
                new(DateTime.Today.AddDays(-2), 100),
                new(DateTime.Today.AddDays(-3), 110)
            }
            };

            // 80 < 90 (drop), 90 < 100 (drop), 100 < 110 (drop) => 3 drops, threshold 2 => should filter out
            Assert.True(filter.ShouldFilterOut(stock));
        }
    }
}
