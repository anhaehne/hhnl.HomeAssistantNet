using System;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant
{
    public class SupervisorConfig
    {
        public string DeployDirectory { get; set; }

        public string SourceDirectory { get; set; }

        public TimeSpan DefaultClientCallTimeout { get; set; } = TimeSpan.FromSeconds(5);
        
        public TimeSpan DefaultProcessExitTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }
}