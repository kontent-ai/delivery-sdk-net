using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Kentico.Kontent.Delivery.Languages
{
    /// <inheritdoc/>
    [DebuggerDisplay("Id = {" + nameof(Id) + "}")]
    public class LanguageSystemAttributes : ILanguageSystemAttributes
    {
        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("id")]
        public string Id { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageSystemAttributes"/> class.
        /// </summary>
        [JsonConstructor]
        public LanguageSystemAttributes()
        {
        }
    }
}
