using System.Collections.Generic;

namespace hhnl.HomeAssistantNet.Shared.Configuration
{
    public class AutomationSecrets : Dictionary<string, string>
    {
        public static AutomationSecrets Empty = new ();
    }
}
