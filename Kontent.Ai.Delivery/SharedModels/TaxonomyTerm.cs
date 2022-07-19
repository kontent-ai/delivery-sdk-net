using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.SharedModels
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(Name) + "}")]
    internal sealed class TaxonomyTerm : ITaxonomyTerm
    {
        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyTerm"/> class.
        /// </summary>
        [JsonConstructor]
        public TaxonomyTerm()
        {
        }
    }
}
