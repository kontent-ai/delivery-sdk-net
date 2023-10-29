using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.Sync
{
    internal sealed class SyncItemData : ISyncItemData
    {
        /// <inheritdoc/>
        [JsonProperty("system")]
        public IContentItemSystemAttributes System { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("elements")]
        public Dictionary<string, object> Elements { get; internal set; }

        /// <summary>
        /// Constructor used for deserialization. Contains no logic.
        /// </summary>
        [JsonConstructor]
        public SyncItemData()
        {
        }
    }
}