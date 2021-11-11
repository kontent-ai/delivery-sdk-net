using System.Diagnostics;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentTypes.Element;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentTypes.Element
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(Name) + "}")]
    internal class ContentElement : IContentElement
    {
        /// <inheritdoc/>
        [JsonProperty("type")]
        public string Type { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename { get; internal set; }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        [JsonConstructor]
        public ContentElement()
        {
        }
    }
}
