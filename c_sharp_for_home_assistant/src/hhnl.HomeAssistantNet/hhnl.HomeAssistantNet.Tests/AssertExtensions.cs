using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace hhnl.HomeAssistantNet.Tests
{
    public static class AssertExtensions
    {
        public static async Task TaskCompletesAsync(this Assert assert, Task t, TimeSpan timeout, string? message = null)
        {
            await Task.WhenAny(t, Task.Delay(timeout));
            
            if(!t.IsCompleted)
                Assert.Fail(message ?? "Task didn't complete in time.");
        }
    }
}