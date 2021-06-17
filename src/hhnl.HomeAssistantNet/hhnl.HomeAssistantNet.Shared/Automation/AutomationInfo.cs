//using System;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 8618 // this is a DTO

namespace hhnl.HomeAssistantNet.Shared.Automation
{
    public class AutomationInfo
    {
        public string Name { get; set; }

        [JsonIgnore]
        public MethodInfo Method { get; set; }

        public bool RunOnStart { get; set; }

        public string DisplayName { get; set; }

        public string? GenerationError { get; set; }

        [JsonIgnore]
        public IReadOnlyCollection<Type> DependsOnEntities { get; set; }

        [JsonIgnore]
        public Func<IServiceProvider, CancellationToken, Task> RunAutomation { get; set; }

        public ReentryPolicy ReentryPolicy { get; set; }
    }
}