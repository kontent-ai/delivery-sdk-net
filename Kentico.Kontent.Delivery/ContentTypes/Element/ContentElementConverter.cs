using Newtonsoft.Json;
using Kentico.Kontent.Delivery.Abstractions;
using System;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.ContentTypes.Element
{
    /// <summary>
    /// Serializes content element definitions into specific types
    /// </summary>
    public class ContentElementConverter : JsonConverter<IContentElement>
    {
        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override IContentElement ReadJson(JsonReader reader, Type objectType, IContentElement existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);

            var elementType = (jObject["type"].ToString()) switch
            {
                "taxonomy" => typeof(TaxonomyElement),
                "multiple_choice" => typeof(MultipleChoiceElement),
                _ => typeof(ContentElement)
            };

            var viewType = serializer.ContractResolver.ResolveContract(elementType);
            var resultInstance = viewType.DefaultCreator();

            serializer.Populate(jObject.CreateReader(), resultInstance);

            return (IContentElement)resultInstance;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, IContentElement value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
