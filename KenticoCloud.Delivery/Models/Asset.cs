using Newtonsoft.Json.Linq;

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
        /// <param name="source">The JSON data to deserialize.</param>
        internal Asset(JToken source)
        {
            Name = source["name"].ToString();
            Type = source["type"].ToString();
            Size = source["size"].Value<int>();
            Url = source["url"].ToString();
        }
    }
}
