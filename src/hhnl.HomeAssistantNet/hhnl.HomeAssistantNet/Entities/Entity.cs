using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Entities
{
    /// <summary>
    /// The entity base class has a few properties that are common among all entities in Home Assistant.
    /// </summary>
    public class Entity
    {
        public Entity(string uniqueId)
        {
            UniqueId = uniqueId;
        }

        /// <summary>
        /// The state of the entity. For example: "on".
        /// </summary>
        public string? State { get; set; }
        
        /// <summary>
        /// Indicate if entity is enabled in the entity registry. It also returns <c>true</c> if the platform doesn't support the entity registry. Disabled entities will not be added to Home Assistant.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Indicate if Home Assistant is able to read the state and control the underlying device.
        /// </summary>
        public bool Available { get; set; } = true;

        /// <summary>
        /// Name of the entity.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// A unique identifier for this entity.
        /// </summary>
        public string? UniqueId { get; }

        /// <summary>
        /// Url of a picture to show for the entity.
        /// </summary>
        public string? EntityPicture { get; set; }

        /// <summary>
        /// Extra classification of what the device is. Each domain specifies their own. Device classes can come with extra requirements for unit of measurement and supported features.
        /// </summary>
        public string? DeviceClass { get; set; }

        /// <summary>
        /// Returns <c>true</c> if the state is based on our assumption instead of reading it from the device.
        /// </summary>
        public bool AssumedState { get; set; }

        /// <summary>
        /// Calls a service.
        /// </summary>
        /// <param name="serviceName">The name of the service</param>
        /// <param name="data">The data passed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected async Task CallServiceAsync(string serviceName, dynamic? data = null, CancellationToken cancellationToken = default)
        {
            
        }
    }
}