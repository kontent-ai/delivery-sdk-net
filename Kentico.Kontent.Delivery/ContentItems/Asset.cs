using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(Name) + "}")]
    public sealed class Asset : IAsset
    {
        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name { get; }

        /// <inheritdoc/>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <inheritdoc/>
        [JsonProperty("type")]
        public string Type { get; }

        /// <inheritdoc/>
        [JsonProperty("size")]
        public int Size { get; }

        /// <inheritdoc/>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <inheritdoc/>
        [JsonProperty("width")]
        public int Width { get; set; }

        /// <inheritdoc/>
        [JsonProperty("height")]
        public int Height { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Asset"/> class.
        /// </summary>
        [JsonConstructor]
        internal Asset(string name, string type, int size, string url, string description, int width, int height)
        {
            Name = name;
            Type = type;
            Size = size;
            Url = url;
            Description = description;
            Width = width;
            Height = height;
        }
    }
}
