namespace hhnl.HomeAssistantNet.Shared.Supervisor
{
    public class ManagementAutomationInfo
    {
        public bool Running { get; set; }

        public string FriendlyName { get; set; }

        public string Name { get; set; }

        public string? LastError { get; set; }
    }
}