using hhnl.HomeAssistantNet.Automations.BuildingBlocks;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.Configuration;
using HomeAssistant;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

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
        [Automation(runOnStart: true)]
        [Schedule(Every.Minute)]
        public async Task TurnOffOfficeWhenTurnedOnBeforeSunSet([NoTrack]Entities.SunSun sun)
        {
            if (sun.State != "below_horizon")
            {

            }
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