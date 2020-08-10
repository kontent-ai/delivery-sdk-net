using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Kentico.Kontent.Delivery.ContentItems.ContentLinks
{
    /// <inheritdoc/>
    [DebuggerDisplay("Codename = {" + nameof(Codename) + "}")]
    internal sealed class ContentLink : IContentLink
    {
        /// <inheritdoc/>
        public string Id
        {
            get; internal set;
        }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename
        {
            get; internal set;
        }

        /// <inheritdoc/>
        [JsonProperty("url_slug")]
        public string UrlSlug
        {
            get; internal set;
        }

        /// <inheritdoc/>
        [JsonProperty("type")]
        public string ContentTypeCodename
        {
            get; internal set;
        }

        [JsonConstructor]
        public ContentLink()
        {
        }
    }
}
