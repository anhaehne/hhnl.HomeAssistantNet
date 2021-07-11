using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Automations.Triggers;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.Entities;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Shared.SourceGenerator;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Tests.Automation
{
    [TestClass]
    public class AutomationInfoProviderTests
    {
        [TestMethod]
        public void DiscoverAutomations_should_return_simple_automation()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(SimpleAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual($"{typeof(SimpleAutomationClass).FullName}.{nameof(Automation)}", automation.Name);
            Assert.AreEqual($"{typeof(SimpleAutomationClass).FullName}.{nameof(Automation)}", automation.DisplayName);
            Assert.AreEqual(0, automation.ListenToEntities.Count);
            Assert.AreEqual(0, automation.SnapshotEntities.Count);
            Assert.AreEqual(0, automation.DependsOnEntities.Count);
            Assert.AreEqual(ReentryPolicy.QueueLatest, automation.ReentryPolicy);
        }

        [TestMethod]
        public void DiscoverAutomations_should_return_simple_async_automation()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(AsyncAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual($"{typeof(AsyncAutomationClass).FullName}.{nameof(Automation)}", automation.Name);
            Assert.AreEqual($"{typeof(AsyncAutomationClass).FullName}.{nameof(Automation)}", automation.DisplayName);
            Assert.AreEqual(0, automation.ListenToEntities.Count);
            Assert.AreEqual(0, automation.SnapshotEntities.Count);
            Assert.AreEqual(0, automation.DependsOnEntities.Count);
            Assert.AreEqual(ReentryPolicy.QueueLatest, automation.ReentryPolicy);
        }

        [TestMethod]
        public void DiscoverAutomations_should_set_display_name()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(DisplayNameAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual($"{typeof(DisplayNameAutomationClass).FullName}.{nameof(Automation)}", automation.Name);
            Assert.AreEqual(DisplayNameAutomationClass.TestValue, automation.DisplayName);
        }

        [TestMethod]
        public void DiscoverAutomations_should_set_reentry_policy()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(ReentryPolicyAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual(ReentryPolicyAutomationClass.TestValue, automation.ReentryPolicy);
        }

        [TestMethod]
        public void DiscoverAutomations_should_discover_inherited_automations()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(InheritedAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual($"{typeof(InheritedAutomationClass).FullName}.{nameof(Automation)}", automation.Name);
            Assert.AreEqual($"{typeof(InheritedAutomationClass).FullName}.{nameof(Automation)}", automation.DisplayName);
            Assert.AreEqual(0, automation.ListenToEntities.Count);
            Assert.AreEqual(0, automation.SnapshotEntities.Count);
            Assert.AreEqual(0, automation.DependsOnEntities.Count);
            Assert.AreEqual(ReentryPolicy.QueueLatest, automation.ReentryPolicy);
        }

        [TestMethod]
        public void DiscoverAutomations_should_set_parameter_entities()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(ParameterAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual(1, automation.ListenToEntities.Count);
            Assert.AreEqual(0, automation.SnapshotEntities.Count);
            Assert.AreEqual(1, automation.DependsOnEntities.Count);

            Assert.IsTrue(automation.ListenToEntities.Contains(typeof(MockEntity1)));
            Assert.IsTrue(automation.DependsOnEntities.Contains(typeof(MockEntity1)));
        }

        [TestMethod]
        public void DiscoverAutomations_should_set_parameter_entities_with_no_track_attribute()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(NoTrackParameterAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual(0, automation.ListenToEntities.Count);
            Assert.AreEqual(0, automation.SnapshotEntities.Count);
            Assert.AreEqual(1, automation.DependsOnEntities.Count);

            Assert.IsFalse(automation.ListenToEntities.Contains(typeof(MockEntity1)));
            Assert.IsTrue(automation.DependsOnEntities.Contains(typeof(MockEntity1)));
        }

        [TestMethod]
        public void DiscoverAutomations_should_set_parameter_entities_with_snapshot_attribute()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(SnapshotParameterAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual(1, automation.SnapshotEntities.Count);
            Assert.IsTrue(automation.SnapshotEntities.Contains(typeof(MockEntity1)));
        }

        [TestMethod]
        public void DiscoverAutomations_should_set_constructor_entities()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(ConstructorEntityAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual(1, automation.DependsOnEntities.Count);
            Assert.AreEqual(0, automation.ListenToEntities.Count);
            Assert.IsTrue(automation.DependsOnEntities.Contains(typeof(MockEntity1)));
        }

        [TestMethod]
        public void DiscoverAutomations_should_set_inhertited_constructor_entities()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(InheritedConstructorEntityAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual(1, automation.DependsOnEntities.Count);
            Assert.AreEqual(0, automation.ListenToEntities.Count);
            Assert.IsTrue(automation.DependsOnEntities.Contains(typeof(MockEntity1)));
        }

        [TestMethod]
        public void DiscoverAutomations_should_ignore_entity_base_classes_in_constructor()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(ConstructorBaseEntityAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual(0, automation.DependsOnEntities.Count);
            Assert.AreEqual(0, automation.ListenToEntities.Count);
        }

        [TestMethod]
        public void DiscoverAutomations_should_ignore_entity_base_classes_in_inherited_constructor()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(InheritedConstructorBaseEntityAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual(1, automation.DependsOnEntities.Count);
            Assert.AreEqual(0, automation.ListenToEntities.Count);
            Assert.IsTrue(automation.DependsOnEntities.Contains(typeof(MockEntity1)));
        }

        [TestMethod]
        public void DiscoverAutomations_should_combine_parameter_and_constructor_entities()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(ParameterAndConstructorEntityAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual(2, automation.DependsOnEntities.Count);
            Assert.AreEqual(1, automation.ListenToEntities.Count);
            Assert.IsTrue(automation.DependsOnEntities.Contains(typeof(MockEntity1)));
            Assert.IsTrue(automation.DependsOnEntities.Contains(typeof(MockEntity2)));
            Assert.IsFalse(automation.ListenToEntities.Contains(typeof(MockEntity1)));
            Assert.IsTrue(automation.ListenToEntities.Contains(typeof(MockEntity2)));
        }

        [TestMethod]
        public void DiscoverAutomations_should_include_any_event_in_snapshot()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(AnyEventAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual(0, automation.DependsOnEntities.Count);
            Assert.AreEqual(0, automation.ListenToEntities.Count);
            Assert.AreEqual(1, automation.SnapshotEntities.Count);
            Assert.IsTrue(automation.SnapshotEntities.Contains(typeof(Events.Any)));
        }

        [TestMethod]
        public void DiscoverAutomations_should_include_current_event_in_snapshot()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(CurrentEventAutomationClass) });

            // Assert
            Assert.AreEqual(1, result.Count);
            var automation = result.First();
            Assert.AreEqual(0, automation.DependsOnEntities.Count);
            Assert.AreEqual(0, automation.ListenToEntities.Count);
            Assert.AreEqual(1, automation.SnapshotEntities.Count);
            Assert.IsTrue(automation.SnapshotEntities.Contains(typeof(Events.Current)));
        }

        [TestMethod]
        public void DiscoverAutomations_should_ignore_private_class()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(PrivateClassAutomationClass) });

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void DiscoverAutomations_should_ignore_static_class()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(StaticClassAutomationClass) });

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void DiscoverAutomations_should_ignore_generic_class()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(GenericClassAutomationClass<>) });

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void DiscoverAutomations_should_ignore_abstract_class()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(AbstractClassAutomationClass) });

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void DiscoverAutomations_should_ignore_private_method()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(PrivateAutomationClass) });

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void DiscoverAutomations_should_ignore_static_method()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(StaticAutomationClass) });

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void DiscoverAutomations_should_ignore_generic_method()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(GenericAutomationClass) });

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void DiscoverAutomations_should_ignore_method_with_non_void_or_task_return_type()
        {
            // Arrange
            var sut = GetSut();

            // Act
            var result = sut.DiscoverAutomations(new[] { typeof(NonValidReturnTypeAutomationClass) });

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        private IAutomationInfoProvider GetSut()
        {
            var metaDataMock = new Mock<IGeneratedMetaData>();
            metaDataMock.SetupGet(x => x.EntityTypes).Returns(new[] { typeof(MockEntity1), typeof(MockEntity2) });

            var sut = new AutomationInfoProvider(Mock.Of<ILogger<AutomationInfoProvider>>(), metaDataMock.Object);
            return sut;
        }

        private class PrivateClassAutomationClass
        {
            [Automation]
            public void Automation()
            {

            }
        }
    }

    public class InheritedConstructorBaseEntityAutomationClass : ConstructorBaseEntityAutomationClass
    {
        public InheritedConstructorBaseEntityAutomationClass(MockEntity1 e) : base(e)
        {
        }
    }

    public class ConstructorBaseEntityAutomationClass
    {
        public ConstructorBaseEntityAutomationClass(Entity l)
        {
        }

        [Automation]
        public void Automation()
        {

        }
    }

    public class InheritedConstructorEntityAutomationClass : ConstructorEntityAutomationClass
    {
        public InheritedConstructorEntityAutomationClass() : base(null!)
        {
        }
    }

    public class CurrentEventAutomationClass
    {
        [Automation]
        public void Automation(Events.Current current)
        {
        }
    }

    public class AnyEventAutomationClass
    {
        [Automation]
        public void Automation(Events.Any any)
        {
        }
    }

    public class ParameterAndConstructorEntityAutomationClass
    {
        public ParameterAndConstructorEntityAutomationClass(MockEntity1 mock)
        {

        }

        [Automation]
        public void Automation(MockEntity2 mockEntity2)
        {
        }
    }

    public class ConstructorEntityAutomationClass
    {
        public ConstructorEntityAutomationClass(MockEntity1 mock)
        {

        }

        [Automation]
        public void Automation()
        {
        }
    }

    public class NonValidReturnTypeAutomationClass
    {
        [Automation]
        public string Automation()
        {
            return "";
        }
    }

    public class GenericAutomationClass
    {
        [Automation]
        public void Automation<T>()
        {

        }
    }

    public class StaticAutomationClass
    {
        [Automation]
        public static void Automation()
        {

        }
    }

    public class PrivateAutomationClass
    {
        [Automation]
        private void Automation()
        {

        }
    }

    public abstract class AbstractClassAutomationClass
    {
        [Automation]
        public void Automation()
        {

        }
    }

    public class GenericClassAutomationClass<T>
    {
        [Automation]
        public void Automation()
        {

        }
    }

    public static class StaticClassAutomationClass
    {
        [Automation]
        public static void Automation()
        {

        }
    }

    public class SnapshotParameterAutomationClass
    {
        [Automation]
        public void Automation([Snapshot] MockEntity1 myEntity1)
        {

        }
    }

    public class NoTrackParameterAutomationClass
    {
        [Automation]
        public void Automation([NoTrack] MockEntity1 myEntity1)
        {

        }
    }

    public class ParameterAutomationClass
    {
        [Automation]
        public void Automation(MockEntity1 myEntity1)
        {

        }
    }

    public class InheritedAutomationClass : SimpleAutomationClass
    {

    }

    public class ReentryPolicyAutomationClass
    {
        public const ReentryPolicy TestValue = ReentryPolicy.Discard;

        [Automation(reentryPolicy: TestValue)]
        public void Automation()
        {

        }
    }

    public class DisplayNameAutomationClass
    {
        public const string TestValue = "test";

        [Automation(TestValue)]
        public void Automation()
        {

        }
    }

    public class AsyncAutomationClass
    {
        [Automation]
        public Task Automation()
        {
            return Task.CompletedTask;
        }
    }

    public class SimpleAutomationClass
    {
        [Automation]
        public void Automation()
        {

        }
    }

    public class MockEntity1 : Entity
    {
        public MockEntity1(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }
    }

    public class MockEntity2 : Entity
    {
        public MockEntity2(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }
    }
}
