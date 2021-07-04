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
        public async Task StartScript([NoTrack]Scripts.Test tst)
        {
            await tst.TurnOnAsync();
        }

        [Automation]
        public async Task OnStartScript([Snapshot]Scripts.Test tst, Event @event)
        {
            if (!tst.IsOn)
                return;

            _logger.LogInformation($"Script turn on works. {tst.IsOn}");
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