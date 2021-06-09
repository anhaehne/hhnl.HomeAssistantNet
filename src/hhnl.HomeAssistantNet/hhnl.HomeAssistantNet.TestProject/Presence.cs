using System;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using HomeAssistant;

namespace hhnl.HomeAssistantNet.TestProject
{
    public class Presence
    {
        private readonly Lights.All _allLights;
        private readonly MediaPlayers.All _allMediaPlayers;
        private readonly IHomeAssistantClient _client;

        public Presence(MediaPlayers.All allMediaPlayers, Lights.All allLights, IHomeAssistantClient client)
        {
            _allMediaPlayers = allMediaPlayers;
            _allLights = allLights;
            _client = client;
        }

        [Automation(runOnStart: true)]
        public async Task AnyoneHome(
            Persons.Andre andre,
            Persons.Janina janina,
            InputBooleans.PresenceGuests guests,
            InputBooleans.PresenceAnyone presenceAnyone,
            CancellationToken ct)
        {
            var isAnyOneHome = andre.IsHome || janina.IsHome || guests.IsOn;
            await presenceAnyone.SetAsync(isAnyOneHome, ct);
        }

        [Automation(runOnStart: true)]
        public async Task LeaveHome(InputBooleans.LeaveHome leaveHome)
        {
            // If leave home is turned off, we have nothing to do.
            if (leaveHome.IsOff)
                return;

            // Turn off all the stuff we want to turn off.

            // Turn of media player
            await _allMediaPlayers.StopAsync();
            // Turn of lights
            await _allLights.TurnOffAsync();

            // Turn of the TV
            await _client.CallServiceAsync("webostv",
                "command",
                new
                {
                    command = "system/turnOff"
                });

            // Wait 5 secs
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Turn off the leave home input
            await leaveHome.TurnOffAsync();
        }
    }
}