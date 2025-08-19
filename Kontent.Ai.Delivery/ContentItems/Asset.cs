using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.ContentItems
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(Name) + "}")]
    internal sealed class Asset : IAsset
    {
        /// <inheritdoc/>
        [JsonPropertyName("name")]
        public string Name { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("description")]
        public string Description { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("type")]
        public string Type { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("size")]
        public int Size { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("url")]
        public string Url { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("width")]
        public int Width { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("height")]
        public int Height { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("renditions")]
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
