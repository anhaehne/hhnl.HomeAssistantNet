using System;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Entities
{
    public abstract class Light : Entity
    {
        public Light(string uniqueId) : base(uniqueId)
        {
        }

        public bool IsOn { get; set; }

        public async Task TurnOffAsync()
        {

        }

        public async Task TurnOnAsync()
        {

        }

        public async Task ToggleAsync()
        {

        }

        [Flags]
        public enum SupportedFeatures
        {
            SupportBrightness = 1,
            SupportColorTemp = 2,
            SupportEffect = 4,
            SupportFlash = 8,
            SupportColor = 16,
            SupportTransition = 32,
            SupportWhiteValue = 128,
        }
    }
}
