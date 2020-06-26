using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.ContentTypes.Element
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(Name) + "}")]
    internal class ContentElement : IContentElement
    {
        string _type, _value, _name, _taxonomyGroup;

        internal JToken Source { get; }

        private IReadOnlyList<IMultipleChoiceOption> _options;

        /// <inheritdoc/>
        [JsonProperty("type")]
        public string Type
        {
            get
            {
                return _type ??= Source?["type"]?.Value<string>();
            }
            internal set
            {
                _type = value;
            }
        }

        /// <inheritdoc/>
        [JsonProperty("value")]
        public string Value
        {
            get
            {
                return _value ??= (Source?["value"] != null ? ((JObject)Source).Property("value").Value.ToString() : null);
            }
            internal set
            {
                _value = value;
            }
        }

        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name
        {
            get
            {
                return _name ??= Source?["name"]?.Value<string>();
            }
            internal set
            {
                _name = value;
            }
        }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("options")]
        public IReadOnlyList<IMultipleChoiceOption> Options
        {
            get
            {
                //todo: add tests for this
                if (_options == null)
                {
                    var source = Source?["options"] ?? new JArray();
                    _options = source.Select(optionSource => optionSource.ToObject<MultipleChoiceOption>()).ToList().AsReadOnly();
                }

                return _options;
            }
            internal set
            {
                _options = value;
            }
        }

        /// <inheritdoc/>
        [JsonProperty("taxonomy_group")]
        public string TaxonomyGroup
        {
            get
            {
                return _taxonomyGroup ??= (Source?["taxonomy_group"]?.Value<string>() ?? string.Empty);
            }
            internal set
            {
                _taxonomyGroup = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentElement"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        /// <param name="codename">The codename of the content element.</param>
        internal ContentElement(JToken source, string codename)
        {
            //TODO: remove
            Source = source;
            Codename = codename;
        }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        [JsonConstructor]
        public ContentElement()
        {
        }
    }
}
