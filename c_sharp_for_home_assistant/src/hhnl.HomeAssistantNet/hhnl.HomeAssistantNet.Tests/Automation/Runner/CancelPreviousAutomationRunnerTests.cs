using System;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation.Runner;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace hhnl.HomeAssistantNet.Tests.Automation.Runner
{
    [TestClass]
    public class CancelPreviousAutomationRunnerTests : AutomationTestBase
    {
        [TestMethod]
        public async Task EnqueueAsync_should_cancel_previous_task()
        {
            // Arrange
            Initialize(true);

            var sut = new CancelPreviousAutomationRunner(Entry, ServiceProvider);
            sut.Start();

            var startTcs = new TaskCompletionSource();
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, startTcs, EmptySnapshot);
            await Assert.That.TaskCompletesAsync(startTcs.Task, TimeSpan.FromSeconds(1), "Run didn't start in time.");

            var firstRun = Entry.LatestRun;
            var firstInstance = await WaitForAutomationInstance(1);

            // Act
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            Assert.AreEqual(2, Entry.Runs.Count, "Second run has not been added.");

            Assert.IsTrue(firstInstance.HasBeenCanceled, "First automation instance wasn't cancelled.");
            Assert.AreEqual(AutomationRunInfo.RunState.Cancelled, firstRun!.State, "First is not in state Cancelled.");
        }
    }
}