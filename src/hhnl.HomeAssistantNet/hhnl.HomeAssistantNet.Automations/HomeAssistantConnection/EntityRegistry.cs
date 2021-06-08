using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Automations.Utils;
using hhnl.HomeAssistantNet.Shared.Entities;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Shared.SourceGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace hhnl.HomeAssistantNet.Automations.HomeAssistantConnection
{
    public class EntityRegistry : IEntityRegistry, IHostedService
    {
        private readonly Dictionary<string, Entity> _entities;
        private readonly IHomeAssistantClient _homeAssistantClient;

        public EntityRegistry(
            IGeneratedMetaData metaData,
            IAutomationRegistry automationRegistry,
            IServiceProvider serviceProvider,
            IHomeAssistantClient homeAssistantClient)
        {
            _homeAssistantClient = homeAssistantClient;

            _entities = metaData.EntityTypes.Select(type => (type, id: GetEntityId(type)))
                .Where(t => automationRegistry.RelevantEntities.Contains(t.id))
                .ToDictionary(x => x.id,
                    x => (Entity)serviceProvider.GetRequiredService(x.type));
        }

        public async Task UpdateEntityAsync(string entityId, JsonElement json)
        {
            await Initialization.WaitForEntitiesLoadedAsync();
            UpdateEntityInternal(entityId, json);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Initialization.WaitForHomeAssistantConnectionAsync();
            
            var json = await _homeAssistantClient.FetchStatesAsync(cancellationToken);

            for (var i = 0; i < json.GetArrayLength(); i++)
            {
                var element = json[i];
                var entityId = element.GetProperty("entity_id").GetString()!;
                UpdateEntityInternal(entityId, element);
            }

            Initialization.EntitiesLoaded();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void UpdateEntityInternal(string entityId, JsonElement json)
        {
            if (!_entities.TryGetValue(entityId, out var result))
                return;

            result.Update(json);
        }

        private string GetEntityId(Type t)
        {
            return t.GetCustomAttribute<UniqueIdAttribute>().Value;
        }
    }

    public interface IEntityRegistry
    {
        public Task UpdateEntityAsync(string entityId, JsonElement json);
    }
}