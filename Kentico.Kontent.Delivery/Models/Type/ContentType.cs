using Kentico.Kontent.Delivery.Abstractions.Models.Type;
using Kentico.Kontent.Delivery.Abstractions.Models.Type.Element;
using Kentico.Kontent.Delivery.Models.Type.Element;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Kentico.Kontent.Delivery.Models.Type
{
    /// <summary>
    /// Represents a content type.
    /// </summary>
    public sealed class ContentType : IContentType
    {
        private readonly JToken _source;
        private ContentTypeSystemAttributes _system;
        private IReadOnlyDictionary<string, IContentElement> _elements;

        /// <summary>
        /// Gets the system attributes of the content type.
        /// </summary>
        public IContentTypeSystemAttributes System
        {
            get
            {
                if (_system == null)
                {
                    _system = _source["system"].ToObject<ContentTypeSystemAttributes>();
                }

                return _system;
            }
        }

        /// <summary>
        /// Gets a dictionary that contains elements of the content type index by their codename.
        /// </summary>
        public IReadOnlyDictionary<string, IContentElement> Elements
        {
            get
            {
                if (_elements == null)
                {
                    var elements = new Dictionary<string, IContentElement>();

                    foreach (JProperty property in _source["elements"])
                    {
                        var element = property.Value;
                        var elementCodename = property.Name;

                        elements.Add(elementCodename, new ContentElement(element, elementCodename));
                    }

                    _elements = new ReadOnlyDictionary<string, IContentElement>(elements);
                }

                return _elements;
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
    }
}
