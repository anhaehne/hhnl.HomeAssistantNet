using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.Entities;
using HomeAssistant;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.TestProject
{
    public abstract class ToggleLightAutomations<T> where T : Light
    {
        [Automation]
        public async Task ToggleLight([NoTrack] T @switch)
        {
            await @switch.ToggleAsync();
        }
    }

    public class OfficeLightAutomations : ToggleLightAutomations<Lights.Buro> { }
}
