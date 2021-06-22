using System;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Automations.Utils
{
    public static class TaskExtensions
    {
        public static async Task IgnoreCancellationAsync(this Task t)
        {
            try
            {
                await t;
            }
            catch (Exception e) when (e is OperationCanceledException || e is TaskCanceledException)
            {
                // ignore
            }
        }

        public static async Task<T> IgnoreCancellationAsync<T>(this Task<T> t, T resultWhenCancelled = default!)
        {
            try
            {
                return await t;
            }
            catch (Exception e) when (e is OperationCanceledException || e is TaskCanceledException)
            {
                // ignore
                return resultWhenCancelled;
            }
        }
    }
}