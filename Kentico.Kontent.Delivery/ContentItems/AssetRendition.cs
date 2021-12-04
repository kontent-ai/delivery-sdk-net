using System;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <inheritdoc/>
    public class AssetRendition : IAssetRendition
    {
        /// <inheritdoc/>
        [JsonProperty("rendition_id")]
        public string RenditionId { get; set; }

        /// <inheritdoc/>
        [JsonProperty("preset_id")]
        public string PresetId { get; set; }

        /// <inheritdoc/>
        [JsonProperty("width")]
        public int Width { get; set; }

        /// <inheritdoc/>
        [JsonProperty("height")]
        public int Height { get; set; }

        /// <inheritdoc/>
        [JsonProperty("query")]
        public string Query { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetRendition"/> class.
        /// </summary>
        [JsonConstructor]
        public AssetRendition()
        {
        }
    }
}