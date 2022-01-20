using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("lock", "Locks", supportsAllEntity: true)]
    public class Lock : ValueEntity<LockState>
    {
        private static readonly Dictionary<string, LockState> _mapping = Enum.GetValues(typeof(LockState)).Cast<LockState>().ToDictionary(x => Enum.GetName(typeof(LockState), x).ToLower());

        public Lock(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        protected override LockState? Parse(string state)
        {
            return _mapping.TryGetValue(state, out var value) ? value : null;
        }

        public async Task LockAsync(CancellationToken cancellationToken = default)
        {
            await HomeAssistantClient.CallServiceAsync("lock", "lock", UniqueId, cancellationToken: cancellationToken);
        }

        public async Task UnlockAsync(CancellationToken cancellationToken = default)
        {
            await HomeAssistantClient.CallServiceAsync("lock", "unlock", UniqueId, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// (unlatch)
        /// </summary>
        public async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            await HomeAssistantClient.CallServiceAsync("lock", "open", UniqueId, cancellationToken: cancellationToken);
        }
    }

    public enum LockState
    {
        Locked,
        Unlocked,
        Locking,
        Unlocking,
        Jammed
    }
}
