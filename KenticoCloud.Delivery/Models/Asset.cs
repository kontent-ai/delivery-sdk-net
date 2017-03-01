using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a digital asset, such as a document or image.
    /// </summary>
    [JsonConverter(typeof(AssetConverter))]
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

    internal class AssetConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Asset));
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var assetJson = JObject.Load(reader);

            return new Asset(assetJson);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
