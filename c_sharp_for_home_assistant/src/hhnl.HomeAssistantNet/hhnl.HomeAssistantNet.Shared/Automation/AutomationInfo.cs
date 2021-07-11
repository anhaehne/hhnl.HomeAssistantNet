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

        public string DisplayName { get; set; }

        [JsonIgnore]
        public string ClassName
        {
            get
            {
                var parts = Name.Split('.');
                return string.Join(".", parts.Take(parts.Length - 1));
            }
        }

        public string? GenerationError { get; set; }

        /// <summary>
        /// The entities that the autmation depends on.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyCollection<Type> DependsOnEntities { get; set; }

        /// <summary>
        /// The entities that trigger a new automation run.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyCollection<Type> ListenToEntities { get; set; }

        /// <summary>
        /// The entities that are served as snapshot.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyCollection<Type> SnapshotEntities { get; set;  }

        [JsonIgnore]
        public Func<IServiceProvider, CancellationToken, Task> RunAutomation { get; set; }

        [JsonIgnore]
        public MethodInfo Method { get; set; }

        public ReentryPolicy ReentryPolicy { get; set; }

    }
}