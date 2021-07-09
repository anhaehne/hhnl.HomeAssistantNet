using hhnl.HomeAssistantNet.Automations.BuildingBlocks;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.Configuration;
using hhnl.HomeAssistantNet.Shared.Entities;
using HomeAssistant;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace hhnl.HomeAssistantNet.TestProject
{
    public class TestAutomation
    {
        private readonly ILogger<TestAutomation> _logger;

        public TestAutomation(ILogger<TestAutomation> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Prevent the lights from being turned on before 8pm.
        /// </summary>
        [Automation]
        public async Task StartScript(Lights.Buro t, Events.Current currentEvent)
        {
        }

        [Automation(runOnStart: true)]
        public async Task MyFirstAutomation([Snapshot]Entities.SunSun sun, Events.Current currentEvent)
        {
            _logger.LogInformation($"This state_change event was triggered by {currentEvent.Context.UserId}");
        }


        [Automation(displayName: "Infinate run automation")]
        public async Task InfiniteRun(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Time.Wait(TimeSpan.FromSeconds(2));
            }
        }

        [Automation]
        public async Task PrintSecrets()
        {
            foreach (var secret in Secrets.GetSecrets())
            {
                _logger.LogInformation($"[{secret.Key}] {secret.Value}");
            }
        }
    }
}