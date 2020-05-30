using Kentico.Kontent.Delivery.Abstractions.Models.Type.Element;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Kentico.Kontent.Delivery.Models.Type.Element
{
    /// <inheritdoc/>
    public class ContentElement : IContentElement
    {
        internal JToken Source { get; private set; }

        private IReadOnlyList<MultipleChoiceOption> _options;

        /// <inheritdoc/>
        public string Type
        {
            get
            {
                return Source.Value<string>("type");
            }
        }

        /// <inheritdoc/>
        public string Value
        {
            get
            {
                return ((JObject)Source).Property("value").Value.ToString();
            }
        }

        /// <inheritdoc/>
        public string Name
        {
            get
            {
                return Source.Value<string>("name");
            }
        }

        /// <inheritdoc/>
        public string Codename { get; }

        /// <inheritdoc/>
        public IReadOnlyList<IMultipleChoiceOption> Options
        {
            get
            {
                if (_options == null)
                {
                    var source = Source["options"] ?? new JArray();
                    _options = source.Select(optionSource => optionSource.ToObject<MultipleChoiceOption>()).ToList().AsReadOnly();
                }

                return _options;
            }
        }

        /// <inheritdoc/>
        public string TaxonomyGroup
        {
            get
            {
                return Source["taxonomy_group"]?.Value<string>() ?? string.Empty;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentElement"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        /// <param name="codename">The codename of the content element.</param>
        internal ContentElement(JToken source, string codename)
        {
            Source = source;
            Codename = codename;
        }
    }
}
