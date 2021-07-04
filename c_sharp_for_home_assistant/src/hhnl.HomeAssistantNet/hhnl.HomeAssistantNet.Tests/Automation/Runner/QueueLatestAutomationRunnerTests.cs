using System;
using System.Linq;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation.Runner;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace hhnl.HomeAssistantNet.Tests.Automation.Runner
{
    [TestClass]
    public class QueueLatestAutomationRunnerTests: AutomationTestBase
    {
        [TestMethod]
        public async Task EnqueueAsync_should_execute_run()
        {
            // Arrange
            Initialize(false);

            var sut = new QueueLatestAutomationRunner(Entry, ServiceProvider);
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

            var sut = new QueueLatestAutomationRunner(Entry, ServiceProvider);
            sut.Start();
            
            await EnqueueRunAsync(sut);

            // Act
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);

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

            var sut = new QueueLatestAutomationRunner(Entry, ServiceProvider);
            sut.Start();
            
            await EnqueueRunAsync(sut);
            var firstAutomationInstance = await WaitForAutomationInstance(1);
            
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);
            var secondRun = Entry.LatestRun;

            // Act
            await firstAutomationInstance.CompleteAndWaitAsync();

            // Assert
            Assert.IsNotNull(secondRun, "No run has been added to the entry.");
            Assert.AreEqual(2, Entry.Runs.Count, "Second run has not been added.");
            Assert.AreEqual(AutomationRunInfo.RunState.Running, Entry.LatestRun!.State, "Second run is not in state running.");
        }
        
        [TestMethod]
        public async Task EnqueueAsync_should_discard_queued_run_when_new_run_is_enqueued()
        {
            // Step 1:
            // Arrange
            Initialize(true);

            var sut = new QueueLatestAutomationRunner(Entry, ServiceProvider);
            sut.Start();
            
            await EnqueueRunAsync(sut);
            var firstAutomationInstance = await WaitForAutomationInstance(1);
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);

            // Act
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            Assert.AreEqual(3, Entry.Runs.Count, "Invalid amount of runs has been added.");
            Assert.AreEqual(AutomationRunInfo.RunState.Cancelled, Entry.Runs.ElementAt(1).State, "Second run wasn't cancelled.");
            Assert.AreEqual(AutomationRunInfo.RunState.WaitingInQueue, Entry.Runs.ElementAt(0).State, "Third run wasn't enqueued.");

            // Step 2:
            // Act 
            await firstAutomationInstance.CompleteAndWaitAsync();

            // Assert
            Assert.AreEqual(AutomationRunInfo.RunState.Running, Entry.Runs.ElementAt(0).State, "Third run wasn't executed.");
        }
        
        [TestMethod]
        public async Task StopAsync_should_cancel_current_run()
        {
            // Arrange
            Initialize(true);

            var sut = new QueueLatestAutomationRunner(Entry, ServiceProvider);
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
            await runner.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, startTcs, EmptySnapshot);
            await Assert.That.TaskCompletesAsync(startTcs.Task, TimeSpan.FromSeconds(1), "Run didn't start in time.");
        }
    }
}