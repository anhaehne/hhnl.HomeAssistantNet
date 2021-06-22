using System;
using System.Linq;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation.Runner;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace hhnl.HomeAssistantNet.Tests.Automation.Runner
{
    [TestClass]
    public class QueueAutomationRunnerTests: AutomationTestBase
    {
        [TestMethod]
        public async Task EnqueueAsync_should_execute_run()
        {
            // Arrange
            Initialize(false);

            var sut = new QueueAutomationRunner(Entry, ServiceProvider);
            sut.Start();

            // Act
            await EnqueueRunAsync(sut);

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            await Assert.That.TaskCompletesAsync(Entry.LatestRun!.Task, TimeSpan.FromSeconds(1));
        }
        
        [TestMethod]
        public async Task EnqueueAsync_should_queue_second_run_when_first_has_not_finished()
        {
            // Arrange
            Initialize(true);

            var sut = new QueueAutomationRunner(Entry, ServiceProvider);
            sut.Start();
            
            await EnqueueRunAsync(sut);

            // Act
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null);

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            Assert.AreEqual(2, Entry.Runs.Count, "Second run has not been added.");
            Assert.AreEqual(AutomationRunInfo.RunState.WaitingInQueue, Entry.LatestRun!.State, "Second run is not in state queued.");
        }
        
        [TestMethod]
        public async Task EnqueueAsync_should_run_second_run_when_first_has_finished()
        {
            // Arrange
            Initialize(true);

            var sut = new QueueAutomationRunner(Entry, ServiceProvider);
            sut.Start();
            
            await EnqueueRunAsync(sut);
            var firstAutomationInstance = await WaitForAutomationInstance(1);
            
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null);
            var secondRun = Entry.LatestRun;

            // Act
            await firstAutomationInstance.CompleteAndWaitAsync();

            // Assert
            Assert.IsNotNull(secondRun, "No run has been added to the entry.");
            Assert.AreEqual(2, Entry.Runs.Count, "Second run has not been added.");
            Assert.AreEqual(AutomationRunInfo.RunState.Running, Entry.LatestRun!.State, "Second run is not in state running.");
        }
        
        [TestMethod]
        public async Task EnqueueAsync_should_discard_run_when_other_run_is_already_queued()
        {
            // Arrange
            Initialize(true);

            var sut = new QueueAutomationRunner(Entry, ServiceProvider);
            sut.Start();
            
            await EnqueueRunAsync(sut);
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null);

            // Act
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null);

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            Assert.AreEqual(2, Entry.Runs.Count, "Third run has been added to the run list.");
            Assert.AreEqual(AutomationRunInfo.RunState.WaitingInQueue, Entry.LatestRun!.State, "Second run is not in state queued.");
        }
        
        [TestMethod]
        public async Task StopAsync_should_cancel_current_run()
        {
            // Arrange
            Initialize(true);

            var sut = new QueueAutomationRunner(Entry, ServiceProvider);
            sut.Start();
            
            await EnqueueRunAsync(sut);

            // Act
            await sut.StopAsync();

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            Assert.AreEqual(AutomationRunInfo.RunState.Cancelled, Entry.LatestRun!.State, "Run is not in state cancelled.");
        }

        private static async Task EnqueueRunAsync(AutomationRunner runner)
        {
            var startTcs = new TaskCompletionSource();
            await runner.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, startTcs);
            await Assert.That.TaskCompletesAsync(startTcs.Task, TimeSpan.FromSeconds(1), "Run didn't start in time.");
        }
    }
}