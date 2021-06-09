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
        public async Task LeaveHome(
            InputBooleans.LeaveHome leaveHome,
            MediaPlayers.All allMediaPlayers,
            Lights.All allLights,
            IHomeAssistantClient client,
            CancellationToken ct)
        {
            // If leave home is turned off, we have nothing to do.
            if (leaveHome.IsOff)
                return;

            // Turn off all the stuff we want to turn off.

            // Turn of media player
            await allMediaPlayers.StopAsync(ct);
            // Turn of lights
            await allLights.TurnOffAsync(ct);

            // Turn of the TV
            await client.CallServiceAsync("webostv",
                "command",
                new
                {
                    command = "system/turnOff"
                },
                ct);

            // Wait 5 secs
            await Task.Delay(TimeSpan.FromSeconds(5), ct);

            // Turn off the leave home input
            await leaveHome.TurnOffAsync(ct);
        }
    }
}