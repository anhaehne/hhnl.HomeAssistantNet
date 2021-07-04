using hhnl.HomeAssistantNet.Automations.Automation.Runner;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Tests.Automation.Runner
{
    [TestClass]
    public class QueueAutomationRunnerTests : AutomationTestBase
    {
        [TestMethod]
        public async Task EnqueueAsync_should_execute_run()
        {
            // Arrange
            Initialize(false);

            QueueAutomationRunner? sut = new QueueAutomationRunner(Entry, ServiceProvider);
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

            QueueAutomationRunner? sut = new QueueAutomationRunner(Entry, ServiceProvider);
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

            QueueAutomationRunner? sut = new QueueAutomationRunner(Entry, ServiceProvider);
            sut.Start();

            await EnqueueRunAsync(sut);
            MockAutomationClass? firstAutomationInstance = await WaitForAutomationInstance(1);

            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);
            AutomationRunInfo? secondRun = Entry.LatestRun;

            // Act
            await firstAutomationInstance.CompleteAndWaitAsync();

            // Assert
            Assert.IsNotNull(secondRun, "No run has been added to the entry.");
            Assert.AreEqual(2, Entry.Runs.Count, "Second run has not been added.");
            Assert.AreEqual(AutomationRunInfo.RunState.Running, Entry.LatestRun!.State, "Second run is not in state running.");
        }

        private static async Task EnqueueRunAsync(AutomationRunner runner)
        {
            TaskCompletionSource? startTcs = new TaskCompletionSource();
            await runner.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, startTcs, EmptySnapshot);
            await Assert.That.TaskCompletesAsync(startTcs.Task, TimeSpan.FromSeconds(1), "Run didn't start in time.");
        }
    }
}