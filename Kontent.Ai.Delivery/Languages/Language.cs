using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.Languages
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(System) + "." + nameof(ILanguageSystemAttributes.Name) + "}")]
    internal sealed class Language : ILanguage
    {
        /// <inheritdoc/>
        [JsonProperty("system")]
        public ILanguageSystemAttributes System { get; internal set; }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        [JsonConstructor]
        public Language(ILanguageSystemAttributes system)
        {
            System = system;
        }
    }
}
