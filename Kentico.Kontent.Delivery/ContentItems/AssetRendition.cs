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
        [JsonProperty("asset_id")]
        public string AssetId { get; set; }

        /// <inheritdoc/>
        [JsonProperty("preset_id")]
        public string PresetId { get; set; }

        /// <inheritdoc/>
        [JsonProperty("size")]
        public long Size { get; set; }

        /// <inheritdoc/>
        [JsonProperty("image_width")]
        public int ImageWidth { get; set; }

        /// <inheritdoc/>
        [JsonProperty("image_height")]
        public int ImageHeight { get; set; }

        /// <inheritdoc/>
        [JsonProperty("transformation_query_string")]
        public string TransformationQueryString { get; set; }

        /// <inheritdoc/>
        [JsonProperty("created")]
        public DateTime Created { get; set; }

        /// <inheritdoc/>
        [JsonProperty("last_modified")]
        public DateTime LastModified { get; set; }
    }
}