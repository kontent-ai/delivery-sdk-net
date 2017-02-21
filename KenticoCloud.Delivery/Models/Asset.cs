using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents an asset.
    /// </summary>
    public class Asset
    {
        /// <summary>
        /// Asset name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Asset type. For example: "image/jpeg".
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Asset size in bytes.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// URL where you can download the asset.
        /// </summary>
        public string Url { get; set; }

        public Asset(JToken asset)
        {
            Name = asset["name"].ToString();
            Type = asset["type"].ToString();
            Size = asset["size"].ToObject<int>();
            Url = asset["url"].ToString();
        }
    }
}