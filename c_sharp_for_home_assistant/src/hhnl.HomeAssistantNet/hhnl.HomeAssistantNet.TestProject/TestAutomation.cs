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
using hhnl.HomeAssistantNet.Automations.Triggers;
using hhnl.HomeAssistantNet.Automations.Automation;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace hhnl.HomeAssistantNet.TestProject
{
    public class TestAutomation
    {
        private readonly ILogger<TestAutomation> _logger;

        public TestAutomation(ILogger<TestAutomation> logger)
        {
            _logger = logger;
        }

        [Automation]
        public async Task StartScript(Lights.Buro t, Events.Current currentEvent)
        {
        }

        [Automation]
        [Schedule(Every.Minute)]
        public async Task Test()
        {
        }

        [Automation]
        [Schedule(WeekDay.All, 20)]
        public async Task Test1()
        {
        }

        [Automation]
        [RunOnConnected]
        public async Task MyFirstAutomation(Sensors.JaninaHandyNachsterWecker wecker)
        {
            var t = wecker.GetValue();
            //_logger.LogInformation($"This state_change event was triggered by {currentEvent.Context.UserId}");
        }

        [Automation]
        [RunOnConnected]
        public async Task MyFirstAutomation2(Events.Current currentEvent)
        {
            //var t = bedroomUi.GetValue();

            //await bedroomUi.SelectOptionAsync(InputSelects.LueftungSteuerungBedroomUi.Options.Aus);
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