using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(Name) + "}")]
    internal sealed class Asset : IAsset
    {
        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("description")]
        public string Description { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("type")]
        public string Type { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("size")]
        public int Size { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("url")]
        public string Url { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("width")]
        public int Width { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("height")]
        public int Height { get; internal set; }
        
        /// <inheritdoc/>
        [JsonProperty("renditions")]
        public Dictionary<string, IAssetRendition> Renditions { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Asset"/> class.
        /// </summary>
        [JsonConstructor]
        public Asset()
        {
        }
    }
}
