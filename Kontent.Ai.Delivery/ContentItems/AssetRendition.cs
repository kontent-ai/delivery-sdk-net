using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems
{
    /// <inheritdoc/>
    public class AssetRendition : IAssetRendition
    {
        /// <inheritdoc/>
        [JsonPropertyName("rendition_id")]
        public string RenditionId { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("preset_id")]
        public string PresetId { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("width")]
        public int Width { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("height")]
        public int Height { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("query")]
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