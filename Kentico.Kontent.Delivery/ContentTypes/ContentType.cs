using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentTypes.Element;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.ContentTypes
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(System) + "." + nameof(IContentTypeSystemAttributes.Name) + "}")]
    internal sealed class ContentType : IContentType
    {
        private readonly JToken _source;
        private IContentTypeSystemAttributes _system;
        private IReadOnlyDictionary<string, IContentElement> _elements;

        /// <inheritdoc/>
        [JsonProperty("system")]
        public IContentTypeSystemAttributes System
        {
            get
            {
                return _system ??= _source["system"].ToObject<ContentTypeSystemAttributes>();
            }
            set
            {
                _system = value;
            }
        }

        /// <inheritdoc/>
        [JsonProperty("elements")]
        public IReadOnlyDictionary<string, IContentElement> Elements
        {
            get
            {
                if (_elements == null)
                {
                    var elements = new Dictionary<string, IContentElement>();

                    foreach (var jToken in _source["elements"])
                    {
                        var property = (JProperty)jToken;
                        var element = property.Value;
                        var elementCodename = property.Name;

                        elements.Add(elementCodename, new ContentElement(element, elementCodename));
                    }

                    _elements = new ReadOnlyDictionary<string, IContentElement>(elements);
                }

                return _elements;
            }
            set
            {
                _elements = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentType"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        internal ContentType(JToken source)
        {
            _source = source;
        }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        [JsonConstructor]
        public ContentType(ContentTypeSystemAttributes system, IReadOnlyDictionary<string, IContentElement> elements)
        {
            System = system;
            Elements = elements;
        }
    }
}
