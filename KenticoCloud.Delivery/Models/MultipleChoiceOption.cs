using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a selected option of a Multiple choice element.
    /// </summary>
    [JsonConverter(typeof(MultipleChoiceOptionConverter))]
    public sealed class MultipleChoiceOption
    {
        /// <summary>
        /// Gets the name of the selected option.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the codename of the selected option.
        /// </summary>
        public string Codename { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleChoiceOption"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        internal MultipleChoiceOption(JToken source)
        {
            Name = source["name"].ToString();
            Codename = source["codename"].ToString();
        }
    }

    internal class MultipleChoiceOptionConverter : JsonConverter
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
            var multipleChoiceOptionJson = JObject.Load(reader);

            return new MultipleChoiceOption(multipleChoiceOptionJson);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
