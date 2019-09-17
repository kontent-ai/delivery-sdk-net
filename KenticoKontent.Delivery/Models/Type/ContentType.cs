using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KenticoKontent.Delivery
{
    /// <summary>
    /// Represents a content type.
    /// </summary>
    public sealed class ContentType
    {
        private readonly JToken _source;
        private ContentTypeSystemAttributes _system;
        private IReadOnlyDictionary<string, ContentElement> _elements;

        /// <summary>
        /// Gets the system attributes of the content type.
        /// </summary>
        public ContentTypeSystemAttributes System
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
        public IReadOnlyDictionary<string, ContentElement> Elements
        {
            get
            {
                if (_elements == null)
                {
                    var elements = new Dictionary<string, ContentElement>();

                    foreach (JProperty property in _source["elements"])
                    {
                        var element = property.Value;
                        var elementCodename = property.Name;

                        elements.Add(elementCodename, new ContentElement(element, elementCodename));
                    }

                    _elements = new ReadOnlyDictionary<string, ContentElement>(elements);
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
