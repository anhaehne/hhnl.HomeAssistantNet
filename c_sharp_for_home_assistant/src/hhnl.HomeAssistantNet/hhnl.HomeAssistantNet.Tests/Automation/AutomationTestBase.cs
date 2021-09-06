using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Automations.Utils;
using hhnl.HomeAssistantNet.Shared.Automation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace hhnl.HomeAssistantNet.Tests.Automation
{
    public abstract class AutomationTestBase
    {
        private List<MockAutomationClass>? _automationClassInstances;
        private AutomationInfo? _automationInfo;
        private AutomationEntry? _entry;
        private IServiceProvider? _serviceProvider;

        protected static IReadOnlyDictionary<Type, object> EmptySnapshot { get; } = new Dictionary<Type, object>();

        protected IServiceProvider ServiceProvider =>
            _serviceProvider ?? throw new InvalidOperationException("Test base not initialized.");

        protected List<MockAutomationClass> AutomationClassInstances => _automationClassInstances ??
                                                                        throw new InvalidOperationException(
                                                                            "Test base not initialized.");

        protected AutomationInfo AutomationInfo => _automationInfo ??
                                                   throw new InvalidOperationException(
                                                       "Test base not initialized.");

        protected AutomationEntry Entry => _entry ??
                                           throw new InvalidOperationException(
                                               "Test base not initialized.");

        protected void Initialize(bool waitForManualCompletion, Exception? throwException = null, bool homeAsisstantClientIsConnected = true)
        {
            if (homeAsisstantClientIsConnected)
                Initialization.HomeAssistantConnected();

            _automationInfo = new AutomationInfo
            {
                RunAutomation = (s, ct) => s.GetRequiredService<MockAutomationClass>().MockAutomation(ct)
            };

            _entry = new AutomationEntry(_automationInfo);

            _automationClassInstances = new List<MockAutomationClass>();

            var services = new ServiceCollection();

            services.AddLogging();
            services.AddMediatR(typeof(AutomationTestBase));

            services.AddTransient(_ =>
            {
                var instance = new MockAutomationClass(waitForManualCompletion, throwException);
                AutomationClassInstances.Add(instance);
                return instance;
            });
            _serviceProvider = services.BuildServiceProvider();
        }

        protected async Task<MockAutomationClass> WaitForAutomationInstance(int count, TimeSpan? timeout = null)
        {
            using var tcs = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(1));
            
            while (AutomationClassInstances.Count != count)
            {
                if(tcs.IsCancellationRequested)
                    Assert.Fail("Waiting for automation class instance reached timeout.");
                
                await Task.Delay(10, tcs.Token).IgnoreCancellationAsync();
            }

            return AutomationClassInstances[count - 1];
        }

        public class MockAutomationClass
        {
            private readonly TaskCompletionSource? _tcs;
            private readonly Exception? _throwException;

            public MockAutomationClass(bool waitForManualCompletion, Exception? throwException = null)
            {
                _throwException = throwException;

                if (waitForManualCompletion)
                    _tcs = new TaskCompletionSource();
            }

            public bool HasBeenCanceled { get; private set; }

            public async Task MockAutomation(CancellationToken cancellationToken)
            {
                if (_throwException is not null)
                    throw _throwException;

                if (_tcs is not null)
                {
                    await using var reg = cancellationToken.Register(() =>
                    {
                        HasBeenCanceled = true;
                        _tcs.TrySetResult();
                    });
                    await _tcs.Task;
                }
            }

            public Task CompleteAndWaitAsync()
            {
                if (_tcs is null)
                    return Task.CompletedTask;

                _tcs.TrySetResult();
                return _tcs.Task;
            }
        }
    }
}