using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation.Runner;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace hhnl.HomeAssistantNet.Tests.Automation.Runner
{
    [TestClass]
    public class DiscardAutomationRunnerTests : AutomationTestBase
    {
        public static IEnumerable<object[]> CancelExceptions { get; } = new[]
        {
            new object[] { new OperationCanceledException() },
            new object[] { new TaskCanceledException() }
        };

        [TestMethod]
        public async Task EnqueueAsync_should_execute_run()
        {
            // Arrange
            Initialize(false);

            var sut = new DiscardAutomationRunner(Entry, ServiceProvider);

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

            var sut = new DiscardAutomationRunner(Entry, ServiceProvider);
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);
            var firstRun = Entry.LatestRun;

            // Act
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            Assert.AreEqual(1, Entry.Runs.Count, "Second run has been added.");
            Assert.AreEqual(firstRun, Entry.LatestRun, "First run is not the latest anymore.");
        }

        [TestMethod]
        public async Task EnqueueAsync_should_run_second_run_when_first_has_finished()
        {
            // Arrange
            Initialize(false);

            var sut = new DiscardAutomationRunner(Entry, ServiceProvider);

            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);
            var firstRun = Entry.LatestRun;
            await Assert.That.TaskCompletesAsync(firstRun!.Task, TimeSpan.FromSeconds(1), "First task didn't complete in time.");

            // Act
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            Assert.AreEqual(2, Entry.Runs.Count, "Second run has not been added.");
            Assert.AreNotEqual(firstRun, Entry.LatestRun, "First run is not the latest anymore.");
        }

        [TestMethod]
        public async Task EnqueueAsync_should_set_run_cancelled_when_home_assistant_client_is_not_connected()
        {
            // Arrange
            Initialize(false, homeAsisstantClientIsConnected: false);

            var sut = new DiscardAutomationRunner(Entry, ServiceProvider);

            // Act
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);
            
            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            Assert.AreNotEqual(AutomationRunInfo.RunState.Cancelled, Entry.LatestRun.State, "Run wasn't cancelled.");
        }

        [TestMethod]
        public async Task StopAsync_should_cancel_current_run()
        {
            // Arrange
            Initialize(true);

            var sut = new DiscardAutomationRunner(Entry, ServiceProvider);

            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);
            
            // Act
            await sut.StopAsync();

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            Assert.IsTrue(AutomationClassInstances.Single().HasBeenCanceled);
            await Assert.That.TaskCompletesAsync(Entry.LatestRun!.Task, TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public async Task StartAutomation_should_set_correct_run_info_on_start()
        {
            // Arrange
            Initialize(true);

            var sut = new DiscardAutomationRunner(Entry, ServiceProvider);

            // Act
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            Assert.AreEqual(AutomationRunInfo.RunState.Running, Entry.LatestRun!.State, "RunState is not running.");
            Assert.AreNotEqual(default, Entry.LatestRun!.Started, "Started has not been set.");
        }

        [TestMethod]
        public async Task StartAutomation_should_set_correct_run_info_on_completion()
        {
            // Arrange
            Initialize(false);

            var sut = new DiscardAutomationRunner(Entry, ServiceProvider);

            // Act
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);
            await Assert.That.TaskCompletesAsync(Entry.LatestRun!.Task, TimeSpan.FromSeconds(1));

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            Assert.IsNotNull(Entry.LatestRun!.Ended, "Ended date hasn't been set.");
            Assert.AreEqual(AutomationRunInfo.RunState.Completed, Entry.LatestRun!.State, "RunState is not completed.");
        }

        [TestMethod]
        public async Task StartAutomation_should_set_correct_run_info_on_exception()
        {
            // Arrange
            var exception = new Exception("Test");
            Initialize(false, exception);

            var sut = new DiscardAutomationRunner(Entry, ServiceProvider);

            //Act 
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            await Assert.That.TaskCompletesAsync(Entry.LatestRun!.Task, TimeSpan.FromSeconds(1));
            Assert.AreEqual(AutomationRunInfo.RunState.Error, Entry.LatestRun!.State, "RunState is not error.");
            Assert.IsNotNull(Entry.LatestRun!.Error, "Error has not been set.");
            Assert.AreEqual(exception.ToString(), Entry.LatestRun!.Error, "Error message is not correct.");
        }

        [TestMethod]
        public Task StartAutomation_should_set_correct_run_info_when_cancelled_by_OperationCanceledException()
            => StartAutomation_should_set_correct_run_info_when_cancelled(new OperationCanceledException());
        
        [TestMethod]
        public Task StartAutomation_should_set_correct_run_info_when_cancelled_by_TaskCanceledException()
            => StartAutomation_should_set_correct_run_info_when_cancelled(new TaskCanceledException());
        
        [TestMethod]
        public async Task StopAsync_should_cancel_runs()
        {
            // Arrange
            Initialize(true);

            var sut = new DiscardAutomationRunner(Entry, ServiceProvider);
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);

            // Act
            await sut.StopAsync();

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            Assert.AreEqual(AutomationRunInfo.RunState.Cancelled, Entry.LatestRun!.State, "Run is not in state cancelled.");
        }

        private async Task StartAutomation_should_set_correct_run_info_when_cancelled(Exception ex)
        {
            // Arrange
            Initialize(false, ex);

            var sut = new DiscardAutomationRunner(Entry, ServiceProvider);

            //Act 
            await sut.EnqueueAsync(AutomationRunInfo.StartReason.Manual, null, null, EmptySnapshot);

            // Assert
            Assert.IsNotNull(Entry.LatestRun, "No run has been added to the entry.");
            await Assert.That.TaskCompletesAsync(Entry.LatestRun!.Task, TimeSpan.FromSeconds(1));
            Assert.AreEqual(AutomationRunInfo.RunState.Cancelled, Entry.LatestRun!.State, "RunState is not cancelled.");
        }
    }
}