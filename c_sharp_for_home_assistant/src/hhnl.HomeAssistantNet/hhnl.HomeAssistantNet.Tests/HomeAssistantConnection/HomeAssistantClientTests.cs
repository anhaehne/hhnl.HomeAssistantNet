using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Automations.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Shared.Configuration;
using hhnl.HomeAssistantNet.Tests.Mocks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Tests.HomeAssistantConnection
{
    [TestClass]
    public class HomeAssistantClientTests
    {
        private HomeAssistantWebSocketServerMock? _serverMock;

        [TestCleanup]
        public async Task TestCleanup()
        {
            await (_serverMock?.StopAsync() ?? Task.CompletedTask);
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


        [TestMethod]
        public async Task StartAsync_should_complete_handshake()
        {
            // Arrange
            var sut = GetSut();

            // Act
            await sut.StartAsync(default);

            // Assert
            await _serverMock!.WaitForAuthHandshakeCompletedAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(_serverMock!.ClientConnected);
        }

        [TestMethod]
        public async Task StartAsync_should_reconnect()
        {
            // Arrange
            var sut = GetSut();
            await sut.StartAsync(default);
            await _serverMock!.WaitForAuthHandshakeCompletedAsync(TimeSpan.FromSeconds(5));

            // Act
            await _serverMock!.StopAsync();
            StartServer();

            // Assert
            await _serverMock!.WaitForAuthHandshakeCompletedAsync(TimeSpan.FromSeconds(999));
            Assert.IsTrue(_serverMock!.ClientConnected);
        }

        private HomeAssistantClient GetSut()
        {
            _serverMock = new HomeAssistantWebSocketServerMock();
            StartServer();

            var haConfig = new HomeAssistantConfig { HOME_ASSISTANT_API = "http://127.0.0.1:32246", SUPERVISOR_TOKEN = HomeAssistantWebSocketServerMock.VALID_TOKEN };

            var client = new HomeAssistantClient(Mock.Of<ILogger<HomeAssistantClient>>(), new OptionsWrapper<HomeAssistantConfig>(haConfig), Mock.Of<IMediator>(), Mock.Of<IAutomationRegistry>());

            return client;
        }

        private void StartServer() => _serverMock!.Start("http://127.0.0.1:32246/api/websocket/");
    }
}
