using STIN_Burza.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STIN_Burza.Tests.Models
{
    public class ErrorViewModelTests
    {
        [Fact]
        public void ShowRequestId_ReturnsTrue_WhenRequestIdIsNotNullOrEmpty()
        {
            var model = new ErrorViewModel { RequestId = "abc123" };
            Assert.True(model.ShowRequestId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ShowRequestId_ReturnsFalse_WhenRequestIdIsNullOrEmpty(string? requestId)
        {
            var model = new ErrorViewModel { RequestId = requestId };
            Assert.False(model.ShowRequestId);
        }
    }
}
