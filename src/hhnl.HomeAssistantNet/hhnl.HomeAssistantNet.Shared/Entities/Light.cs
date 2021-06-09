using System;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("light", "Lights", typeof(SupportedFeatures), true)]
    public abstract class Light : OnOffEntity
    {
        [Flags]
        public enum SupportedFeatures
        {
            SupportBrightness = 1,
            SupportColorTemp = 2,
            SupportEffect = 4,
            SupportFlash = 8,
            SupportColor = 16,
            SupportTransition = 32,
            SupportWhiteValue = 128
        }

        public Light(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient, "light")
        {
        }
    }
}