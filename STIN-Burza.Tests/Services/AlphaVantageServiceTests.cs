using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using STIN_Burza.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace STIN_Burza.Tests.Services
{
    public class AlphaVantageServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IMyLogger> _mockLogger;
        private readonly HttpClient _httpClient;
        private readonly AlphaVantageService _service;
        private readonly Mock<HttpMessageHandler> _mockMessageHandler;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

        public AlphaVantageServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<IMyLogger>();
            _mockMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _httpClient = new HttpClient(_mockMessageHandler.Object);
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);

            _mockConfig.Setup(config => config["AlphaVantage:ApiKey"]).Returns("test_api_key");
            _mockConfig.Setup(config => config["Configuration:WorkingDaysBackValues"]).Returns("7");

            //_service = new AlphaVantageService(_mockConfig.Object, _mockLogger.Object, _mockHttpClientFactory.Object);
        }

        
    }
}
