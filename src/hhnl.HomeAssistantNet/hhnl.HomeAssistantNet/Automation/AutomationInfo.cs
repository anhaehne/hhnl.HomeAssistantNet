using System.Collections.Generic;
using System.Reflection;
#pragma warning disable 8618 // this is a DTO

namespace hhnl.HomeAssistantNet.Automation
{
    public class AutomationInfo
    {
        public MethodInfo Method { get; set; }

        public bool RunOnStart { get; set; }

        public string DisplayName { get; set; }

        public IReadOnlyCollection<string> DependentEntities { get; set; }
    }
}