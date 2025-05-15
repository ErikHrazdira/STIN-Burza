using STIN_Burza.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace STIN_Burza.Tests.Services
{
    public class MyLoggerTests
    {
        [Fact]
        public void LogAndGetLastLines_WritesAndReadsLogFile()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var logger = new MyLogger(tempFile);

            // Act
            logger.Log("Test message 1");
            logger.Log("Test message 2");
            var lines = logger.GetLastLines(2);

            // Assert
            Assert.Equal(2, lines.Count);
            Assert.Contains("Test message 1", lines[0]);
            Assert.Contains("Test message 2", lines[1]);

            // Cleanup
            File.Delete(tempFile);
        }

        [Fact]
        public void GetLastLines_ReturnsEmptyList_WhenFileDoesNotExist()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".log");
            var logger = new MyLogger(tempFile);

            // Act
            var lines = logger.GetLastLines();

            // Assert
            Assert.Empty(lines);
        }
    }
}
