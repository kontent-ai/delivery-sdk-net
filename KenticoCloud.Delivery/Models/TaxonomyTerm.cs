using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a taxonomy term assigned to a Taxonomy element.
    /// </summary>
    [JsonConverter(typeof(TaxonomyTermConverter))]
    public sealed class TaxonomyTerm
    {
        /// <summary>
        /// Gets the name of the taxonomy term.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the codename of the taxonomy term.
        /// </summary>
        public string Codename { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyTerm"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        internal TaxonomyTerm(JToken source)
        {
            Name = source["name"].ToString();
            Codename = source["codename"].ToString();
        }
    }

    internal class TaxonomyTermConverter : JsonConverter
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
            var taxonomyTermJson = JObject.Load(reader);

            return new TaxonomyTerm(taxonomyTermJson);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
