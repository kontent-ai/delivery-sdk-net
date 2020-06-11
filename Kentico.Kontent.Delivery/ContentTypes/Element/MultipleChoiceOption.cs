using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentTypes.Element
{
    /// <inheritdoc/>
    public sealed class MultipleChoiceOption : IMultipleChoiceOption
    {
        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name
        {
            get;
        }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleChoiceOption"/> class with the specified JSON data.
        /// </summary>
        /// <param name="name">Name of the option.</param>
        /// <param name="codename">Code name of the option.</param>
        [JsonConstructor]
        internal MultipleChoiceOption(string name, string codename)
        {
            Name = name;
            Codename = codename;
        }
    }
}
