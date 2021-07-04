using System;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation.Runner;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace hhnl.HomeAssistantNet.Tests.Automation.Runner
{
    [TestClass]
    public class AllowAutomationRunnerTests : AutomationTestBase
    {
        [TestMethod]
        public async Task EnqueueAsync_should_execute_run()
        {
            // Arrange
            Initialize(false);

            var sut = new AllowAutomationRunner(Entry, ServiceProvider);

            // Act
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            await Assert.That.TaskCompletesAsync(Entry.LatestRun!.Task, TimeSpan.FromSeconds(1));
        }
        
        
        [TestMethod]
        public async Task EnqueueAsync_should_discard_second_run_when_first_has_not_finished()
        {
            // Arrange
            Initialize(true);

            var sut = new AllowAutomationRunner(Entry, ServiceProvider);
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);
            var firstRun = Entry.LatestRun;

            // Act
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            Assert.AreEqual(2, Entry.Runs.Count, "Second run has not been added.");
            Assert.AreNotEqual(firstRun, Entry.LatestRun, "First run is not the latest anymore.");
        }
        
        [TestMethod]
        public async Task StopAsync_should_cancel_runs()
        {
            // Arrange
            Initialize(true);

            var sut = new AllowAutomationRunner(Entry, ServiceProvider);
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);

            // Act
            await sut.StopAsync();

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            Assert.AreEqual(AutomationRunInfo.RunState.Cancelled, Entry.LatestRun!.State, "Run is not in state cancelled.");
        }
    }
}