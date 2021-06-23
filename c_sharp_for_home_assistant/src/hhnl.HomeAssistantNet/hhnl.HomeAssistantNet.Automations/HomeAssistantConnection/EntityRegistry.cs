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
using Microsoft.Extensions.Logging;

namespace hhnl.HomeAssistantNet.Automations.HomeAssistantConnection
{
    public class EntityRegistry : IEntityRegistry, IHostedService
    {
        private static readonly JsonElement _allEntity = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
        {
            entity_id = Entity.AllEntityId,
            attributes = new
            {
                friendly_name = "all. Represents all entities of this type.",
                supported_features = -1
            }
        }));

        private readonly Dictionary<string, Entity> _entities;
        private readonly IHomeAssistantClient _homeAssistantClient;
        private readonly ILogger<EntityRegistry> _logger;

        public EntityRegistry(
            IGeneratedMetaData metaData,
            IAutomationRegistry automationRegistry,
            IServiceProvider serviceProvider,
            IHomeAssistantClient homeAssistantClient,
            ILogger<EntityRegistry> logger)
        {
            _homeAssistantClient = homeAssistantClient;
            _logger = logger;

            var entityById = metaData.EntityTypes.Select(type => (type, id: GetEntityId(type))).ToList();

            var allEntities = entityById.Where(t => t.id == Entity.AllEntityId).ToList();

            _entities = entityById.Except(allEntities).Where(t => automationRegistry.RelevantEntities.Contains(t.id))
                .ToDictionary(x => x.id,
                    x => (Entity)serviceProvider.GetRequiredService(x.type));

            // Initialize all entities with a default state.
            foreach (var allEntity in allEntities)
            {
                var entity = (Entity)serviceProvider.GetRequiredService(allEntity.type);
                entity.CurrentState = _allEntity;
            }
        }

        public async Task UpdateEntityAsync(string entityId, JsonElement json)
        {
            await Initialization.WaitForEntitiesLoadedAsync();
            UpdateEntityInternal(entityId, json);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Initialization.WaitForHomeAssistantConnectionAsync();

            _logger.LogInformation("Fetching entity states.");
            
            var json = await _homeAssistantClient.FetchStatesAsync(cancellationToken);

            _logger.LogInformation($"Received {json.GetArrayLength()} entities.");
            
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

            result.CurrentState = json;
        }

        private string GetEntityId(Type t)
        {
            return t.GetCustomAttribute<UniqueIdAttribute>()!.Value;
        }
    }

    public interface IEntityRegistry
    {
        public Task UpdateEntityAsync(string entityId, JsonElement json);
    }
}