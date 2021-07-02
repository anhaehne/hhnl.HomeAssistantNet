using System;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant
{
#pragma warning disable 8618
    public class SupervisorConfig
    {
        public string DeployDirectory { get; set; }

        public string SourceDirectory { get; set; }

        public string BuildDirectory { get; set; }

        public string ConfigDirectory { get; set; }

        public TimeSpan DefaultClientCallTimeout { get; set; } = TimeSpan.FromSeconds(5);
        
        public TimeSpan DefaultProcessExitTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public bool SuppressAutomationDeploy { get; set; }
    }
}