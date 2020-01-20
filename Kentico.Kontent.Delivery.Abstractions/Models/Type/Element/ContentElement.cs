using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a content element.
    /// </summary>
    public class ContentElement
    {
        private readonly JToken _source;
        private readonly string _codename;

        private IReadOnlyList<MultipleChoiceOption> _options;

        /// <summary>
        /// Gets the type of the content element, for example "multiple_choice".
        /// </summary>
        public string Type
        {
            get
            {
                return _source.Value<string>("type");
            }
        }

        /// <summary>
        /// Gets the name of the content element.
        /// </summary>
        public string Name
        {
            get
            {
                return _source.Value<string>("name");
            }
        }

        /// <summary>
        /// Gets the codename of the content element.
        /// </summary>
        public string Codename
        {
            get
            {
                return _codename;
            }
        }

        /// <summary>
        /// Gets a list of predefined options for the Multiple choice content element; otherwise, an empty list.
        /// </summary>
        public IReadOnlyList<MultipleChoiceOption> Options {
            get
            {
                if (_options == null)
                {
                    var source = _source["options"] ?? new JArray();
                    _options = source.Select(optionSource => optionSource.ToObject<MultipleChoiceOption>()).ToList().AsReadOnly();
                }

                return _options;
            }
        }

        /// <summary>
        /// Gets the codename of the taxonomy group for the Taxonomy content element; otherwise, an empty string.
        /// </summary>
        public string TaxonomyGroup
        {
            get
            {
                return _source["taxonomy_group"]?.Value<string>() ?? string.Empty;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentElement"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        /// <param name="codename">The codename of the content element.</param>
        internal ContentElement(JToken source, string codename)
        {
            _source = source;
            _codename = codename;
        }
    }
}
