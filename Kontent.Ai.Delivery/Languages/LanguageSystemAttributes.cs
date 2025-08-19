using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.Languages
{
    /// <inheritdoc/>
    [DebuggerDisplay("Id = {" + nameof(Id) + "}")]
    public class LanguageSystemAttributes : ILanguageSystemAttributes
    {
        /// <inheritdoc/>
        [JsonPropertyName("codename")]
        public string Codename { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("id")]
        public string Id { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("name")]
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
