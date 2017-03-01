using Newtonsoft.Json;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a digital asset, such as a document or image.
    /// </summary>
    public sealed class Asset
    {
        /// <summary>
        /// Gets the name of the asset.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the media type of the asset, for example "image/jpeg".
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Gets the asset size in bytes.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Gets the URL of the asset.
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Asset"/> class with the specified JSON data.
        /// </summary>
        [JsonConstructor]
        internal Asset(string name, string type, int size, string url)
        {
            Name = name;
            Type = type;
            Size = size;
            Url = url;
        }
    }
}
