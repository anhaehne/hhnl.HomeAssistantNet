using Nito.AsyncEx;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Automations.Utils
{
    public static class Initialization
    {
        private static readonly AsyncManualResetEvent _homeAssistantConnection = new();
        private static readonly AsyncManualResetEvent _entitiesLoaded = new();

        public static async Task WaitForHomeAssistantConnectionAsync()
        {
            await _homeAssistantConnection.WaitAsync();
        }

        public static async Task WaitForEntitiesLoadedAsync()
        {
            await _entitiesLoaded.WaitAsync();
        }

        public static void HomeAssistantConnected()
        {
            _homeAssistantConnection.Set();
        }

        public static void HomeAssistantDisconnected()
        {
            _homeAssistantConnection.Reset();
        }

        public static bool IsHomeAssistantConnected => _homeAssistantConnection.IsSet;

        public static void EntitiesLoaded()
        {
            _entitiesLoaded.Set();
        }
    }
}