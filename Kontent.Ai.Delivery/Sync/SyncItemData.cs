using System.Collections.Generic;
using System.Linq;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentTypes.Element;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.Sync
{
    public class SyncItemData : ISyncItemData
    {
        [JsonProperty("system")]
        public IContentItemSystemAttributes System { get; internal set; }

        [JsonProperty("elements")]
        public object Elements { get; internal set; }

        [JsonConstructor]
        public SyncItemData()
        {
        }
    }
}