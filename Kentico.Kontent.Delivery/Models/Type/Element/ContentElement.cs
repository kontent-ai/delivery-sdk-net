using System.Collections.Generic;
using System.Linq;
using Kentico.Kontent.Delivery.Abstractions.Models.Type.Element;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.Models.Type.Element
{
    /// <inheritdoc/>
    public class ContentElement : IContentElement
    {
        internal JToken Source { get; }

        private IReadOnlyList<MultipleChoiceOption> _options;

        /// <inheritdoc/>
        public string Type => Source["type"]?.Value<string>() ?? null;

        /// <inheritdoc/>
        public string Value => Source["value"] != null ? ((JObject)Source).Property("value").Value.ToString() : null;

        /// <inheritdoc/>
        public string Name => Source["name"]?.Value<string>() ?? null;

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
        public string TaxonomyGroup => Source["taxonomy_group"]?.Value<string>() ?? string.Empty;

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
