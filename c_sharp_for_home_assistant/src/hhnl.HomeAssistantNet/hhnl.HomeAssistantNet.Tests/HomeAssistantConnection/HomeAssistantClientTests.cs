using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Automations.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Shared.Configuration;
using hhnl.HomeAssistantNet.Tests.Mocks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Tests.HomeAssistantConnection
{
    [TestClass]
    public class HomeAssistantClientTests
    {
        private HomeAssistantWebSocketServerMock? _serverMock;

        [TestCleanup]
        public void TestCleanup()
        {
            _serverMock?.Stop();
        }

        [TestMethod]
        public async Task StartAsync_should_connect()
        {
            // Arrange
            var sut = GetSut();

            // Act
            await sut.StartAsync(default);

            // Assert
            Assert.IsTrue(_serverMock!.ClientConnected);
        }

        private HomeAssistantClient GetSut()
        {
            _serverMock = new HomeAssistantWebSocketServerMock();
            _serverMock.Start("http://127.0.0.1:32246/api/websocket/");

            var haConfig = new HomeAssistantConfig { HOME_ASSISTANT_API = "http://127.0.0.1:32246", SUPERVISOR_TOKEN = HomeAssistantWebSocketServerMock.VALID_TOKEN };

            var client = new HomeAssistantClient(Mock.Of<ILogger<HomeAssistantClient>>(), new OptionsWrapper<HomeAssistantConfig>(haConfig), Mock.Of<IMediator>(), Mock.Of<IAutomationRegistry>());

            return client;
        }
    }
}
