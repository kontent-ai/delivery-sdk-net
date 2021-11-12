using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using System.Diagnostics;
using Kentico.Kontent.Delivery.Abstractions.SharedModels;

namespace Kentico.Kontent.Delivery.SharedModels
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(Name) + "}")]
    internal sealed class MultipleChoiceOption : IMultipleChoiceOption
    {
        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleChoiceOption"/> class with the specified JSON data.
        /// </summary>
        [JsonConstructor]
        public MultipleChoiceOption()
        {
        }
    }
}
