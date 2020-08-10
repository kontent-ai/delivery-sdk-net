using Newtonsoft.Json;
using Kentico.Kontent.Delivery.Abstractions;
using System;
using Newtonsoft.Json.Linq;
using Kentico.Kontent.Delivery.ContentItems.Elements;

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

            objectType = (jObject["type"].ToString()) switch
            {
                "taxonomy" => typeof(TaxonomyElement),
                "multiple_choice" => typeof(MultipleChoiceElement),
                _ => typeof(ContentElement)
            };

            var viewType = serializer.ContractResolver.ResolveContract(objectType);
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
