using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.Entities;
using hhnl.HomeAssistantNet.Shared.SourceGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Tests.Automation
{
    [TestClass]
    public class AutomationInfoProviderRunTests
    {
        [TestMethod]
        public async Task RunAutomation_should_run_automation()
        {
            // Act 
            var instance = await RunAutomationFromClass<SimpleAutomationRunClass>();

            // Assert
            Assert.IsTrue(instance.HasBeenExecuted);
        }

        [TestMethod]
        public async Task RunAutomation_should_pass_cancellation_token()
        {
            using var cts = new CancellationTokenSource();

            // Act 
            var instance = await RunAutomationFromClass<CancellationTokenAutomationRunClass>(ct: cts.Token);

            // Assert
            Assert.AreEqual(cts.Token, instance.CancellationToken);
        }

        [TestMethod]
        public async Task RunAutomation_should_pass_entity()
        {
            var entity = new MockEntity1(null!, null!);

            // Act 
            var instance = await RunAutomationFromClass<EntityAutomationRunClass>(new[] { entity });

            // Assert
            Assert.AreEqual(entity, instance.Entity);
        }

        [TestMethod]
        public async Task RunAutomation_should_pass_snapshot_entity()
        {
            var entity = new MockEntity1(null!, null!);

            // Act 
            var instance = await RunAutomationFromClass<SnapshotEntityAutomationRunClass>(snapshotProviderTypes: new[] { entity });

            // Assert
            Assert.AreEqual(entity, instance.Entity);
        }

        [TestMethod]
        public async Task RunAutomation_should_pass_any_event()
        {
            var @event = new Events.Any(new Events.Current(default, default!, default, default!, default!));

            // Act 
            var instance = await RunAutomationFromClass<AnyEventAutomationRunClass>(snapshotProviderTypes: new[] { @event });

            // Assert
            Assert.AreEqual(@event, instance.Any);
        }

        [TestMethod]
        public async Task RunAutomation_should_pass_current_event()
        {
            var @event = new Events.Current(default, default!, default, default!, default!);

            // Act 
            var instance = await RunAutomationFromClass<CurrentEventAutomationRunClass>(snapshotProviderTypes: new[] { @event });

            // Assert
            Assert.AreEqual(@event, instance.Current);
        }

        private async Task<TAutomationClass> RunAutomationFromClass<TAutomationClass>(IEnumerable<object>? serviceProviderTypes = null, IEnumerable<object>? snapshotProviderTypes = null, CancellationToken ct = default) where TAutomationClass : class
        {
            var sut = GetSut();
            var automation = sut.DiscoverAutomations(new[] { typeof(TAutomationClass) }).Single();
            var serviceProvider = GetServiceProvider<TAutomationClass>(serviceProviderTypes, snapshotProviderTypes);

            // Act
            await automation.RunAutomation(serviceProvider, ct);

            return serviceProvider.GetRequiredService<TAutomationClass>();
        }

        private IAutomationInfoProvider GetSut()
        {
            var metaDataMock = new Mock<IGeneratedMetaData>();
            metaDataMock.SetupGet(x => x.EntityTypes).Returns(new[] { typeof(MockEntity1), typeof(MockEntity2) });

            var sut = new AutomationInfoProvider(Mock.Of<ILogger<AutomationInfoProvider>>(), metaDataMock.Object);
            return sut;
        }

        private IServiceProvider GetServiceProvider<TAutomationClass>(IEnumerable<object>? serviceProviderTypes = null, IEnumerable<object>? snapshotProviderTypes = null) where TAutomationClass : class
        {
            var services = new ServiceCollection();
            services.AddSingleton<TAutomationClass>();

            if(serviceProviderTypes is not null)
            {
                foreach (var t in serviceProviderTypes)
                {
                    services.AddSingleton(t.GetType(), t);
                }
            }

            var providerTypes = (snapshotProviderTypes ?? Enumerable.Empty<object>()).ToDictionary(x => x.GetType());

            var snapshotProviderMock = new Mock<IEntitySnapshotProvider>();
            snapshotProviderMock.Setup(x => x.GetSnapshot(It.IsAny<Type>())).Returns<Type>(x => providerTypes[x]);

            services.AddSingleton<IEntitySnapshotProvider>(snapshotProviderMock.Object);

            return services.BuildServiceProvider();
        }
    }
    public class CurrentEventAutomationRunClass
    {
        public Events.Current? Current { get; private set; }

        [Automation]
        public void Automation(Events.Current current)
        {
            Current = current;
        }
    }

    public class AnyEventAutomationRunClass
    {
        public Events.Any? Any { get; private set; }

        [Automation]
        public void Automation(Events.Any any)
        {
            Any = any;
        }
    }

    public class SnapshotEntityAutomationRunClass
    {
        public MockEntity1? Entity { get; private set; }

        [Automation]
        public void Automation([Snapshot]MockEntity1 e)
        {
            Entity = e;
        }
    }

    public class EntityAutomationRunClass
    {
        public MockEntity1? Entity { get; private set; }

        [Automation]
        public void Automation(MockEntity1 e)
        {
            Entity = e;
        }
    }

    public class CancellationTokenAutomationRunClass
    {
        public CancellationToken CancellationToken { get; private set; }

        [Automation]
        public void Automation(CancellationToken ct)
        {
            CancellationToken = ct;
        }
    }

    public class SimpleAutomationRunClass
    {
        public bool HasBeenExecuted { get; private set; }

        [Automation]
        public void Automation()
        {
            HasBeenExecuted = true;
        }
    }
}
