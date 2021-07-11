using hhnl.HomeAssistantNet.Automations.Triggers;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.Entities;
using hhnl.HomeAssistantNet.Shared.SourceGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public interface IAutomationInfoProvider
    {
        IReadOnlyCollection<AutomationInfo> DiscoverAutomations(IEnumerable<Type> assemblyTypes);
    }

    public class AutomationInfoProvider : IAutomationInfoProvider
    {
        private readonly ILogger<AutomationInfoProvider> _logger;
        private readonly IGeneratedMetaData _generatedMetaData;

        public AutomationInfoProvider(ILogger<AutomationInfoProvider> logger, IGeneratedMetaData generatedMetaData)
        {
            _logger = logger;
            _generatedMetaData = generatedMetaData;
        }

        public IReadOnlyCollection<AutomationInfo> DiscoverAutomations(IEnumerable<Type> assemblyTypes)
        {
            List<AutomationInfo> automations = new();
            var results = DiscoverAutomationMethods(assemblyTypes);

            foreach (var (type, automationMethod) in results)
            {
                var (isValid, error) = Validate(type, automationMethod);

                if (!isValid)
                {
                    if (error is not null)
                        _logger.LogWarning($"Ignoring automation method {automationMethod}. Reason: '{error}'");

                    continue;
                }


                automations.Add(ToAutomationInfo(type, automationMethod));
            }

            return automations;
        }

        public static IReadOnlyCollection<Type> GetAutomationClasses(IEnumerable<Type> assemblyTypes)
        {
            return DiscoverAutomationMethods(assemblyTypes).Where(r => Validate(r.Type, r.Method).IsValid).Select(r => r.Type).Distinct().ToList();
        }

        private static IEnumerable<(Type Type, MethodInfo Method)> DiscoverAutomationMethods(IEnumerable<Type> assemblyTypes)
        {
            return assemblyTypes.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy).Select(m => (Type: t, Method: m))).Where(x => x.Method.GetCustomAttribute<AutomationAttribute>() is not null);
        }

        private AutomationInfo ToAutomationInfo(Type type, MethodInfo method)
        {
            var attribute = method.GetCustomAttribute<AutomationAttribute>()!;

            var classDependencies = GetClassEntityDependencies(type);
            if (method.DeclaringType is not null && method.DeclaringType != type)
                classDependencies = classDependencies.Concat(GetClassEntityDependencies(method.DeclaringType)).Distinct();

            var methodParameterInfo = GetMethodEntityDependencies(method).ToList();

            var entityDependencies = classDependencies.Concat(methodParameterInfo.Where(x => x.IsEntity).Select(x => x.Type));
            var listenToEntities = methodParameterInfo.Where(x => x.IsEntity && !x.NoTrack).Select(x => x.Type);

            var snapshotTypes = methodParameterInfo.Where(x => x.Snapshot).Select(x => x.Type);

            var info = new AutomationInfo
            {
                Name = $"{type}.{method.Name}",
                DisplayName = attribute.DisplayName ?? $"{type}.{method.Name}",
                ReentryPolicy = attribute.ReentryPolicy,
                DependsOnEntities = entityDependencies.ToList(),
                ListenToEntities = listenToEntities.ToList(),
                SnapshotEntities = snapshotTypes.ToList(),
                Method = method,

                // TODO: optimize
                RunAutomation = async (services, ct) =>
                {
                    var instance = services.GetRequiredService(type);
                    var args = methodParameterInfo.Select(parameter =>
                    {
                        if (parameter.Type == typeof(CancellationToken))
                            return ct;

                        if(parameter.Snapshot)
                            return services.GetRequiredService<IEntitySnapshotProvider>().GetSnapshot(parameter.Type);

                        return services.GetRequiredService(parameter.Type);
                    }).ToArray();
                    var result = method.Invoke(instance, args);

                    if (result is Task t)
                        await t;
                }
            };

            return info;
        }

        private static (bool IsValid, string? Error) Validate(Type type, MethodInfo method)
        {
            var typeResult = Validate(type);

            if (!typeResult.IsValid)
                return typeResult;

            // Validate method
            if (!method.IsPublic)
                return (false, "Automation methods must be public.");

            if (method.IsStatic)
                return (false, "Automation methods can't be static.");

            if (method.IsGenericMethod)
                return (false, "Automation methods can't be generic.");

            if (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task))
                return (false, "Automation methods must either return 'void' or 'Task'.");

            return (true, null);
        }

        private static (bool IsValid, string? Error) Validate(Type type)
        {
            if (!type.IsPublic)
                return (false, "Classes containing automation methods must be public.");

            if (type.IsGenericType && !type.IsAbstract)
                return (false, "Generic classes containing automation methods must be declared abstract.");

            if (type.IsAbstract && type.IsSealed)
                return (false, "Classes containing automation methods can't be static.");

            // Ignore abstract classes
            if (type.IsAbstract)
                return (false, null);

            return (true, null);
        }

        private IEnumerable<Type> GetClassEntityDependencies(Type @class)
        {
            return @class.GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType).Distinct().Where(p => _generatedMetaData.EntityTypes.Contains(p));
        }

        private IEnumerable<(Type Type, bool NoTrack, bool Snapshot, bool IsEntity)> GetMethodEntityDependencies(MethodInfo method)
        {
            foreach (var parameter in method.GetParameters())
            {
                // If the soure event is requested, we include it in the snapshot
                if(IsEventType(parameter.ParameterType))
                {
                    yield return (parameter.ParameterType, false, true, false);
                    continue;
                }

                yield return (parameter.ParameterType, HasNoTrackAttribute(parameter), HasSnapshotAttribute(parameter), _generatedMetaData.EntityTypes.Contains(parameter.ParameterType));
            }
        }

        private static bool HasSnapshotAttribute(ParameterInfo parameter) => parameter.GetCustomAttribute<SnapshotAttribute>() is not null;
        private static bool HasNoTrackAttribute(ParameterInfo parameter) => parameter.GetCustomAttribute<NoTrackAttribute>() is not null;

        private static bool IsEventType(Type t) => t == typeof(Events.Any) || t == typeof(Events.Current);
    }
}
