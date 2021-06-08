using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Automations.Utils
{
    public static class Initialization
    {
        private static readonly TaskCompletionSource<bool> _homeAssistantConnection = new();
        private static readonly TaskCompletionSource<bool> _entitiesLoaded = new();

        public static async Task WaitForHomeAssistantConnectionAsync()
        {
            await _homeAssistantConnection.Task;
        }

        public static async Task WaitForEntitiesLoadedAsync()
        {
            await _entitiesLoaded.Task;
        }

        public static void HomeAssistantConnected()
        {
            _homeAssistantConnection.SetResult(true);
        }

        public static void EntitiesLoaded()
        {
            _entitiesLoaded.SetResult(true);
        }
    }
}