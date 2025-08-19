using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.ContentItems.ContentLinks
{
    /// <inheritdoc/>
    [DebuggerDisplay("Codename = {" + nameof(IContentLink.Codename) + "}")]
    internal sealed class ContentLink : IContentLink
    {
        /// <inheritdoc/>
        Guid IContentLink.Id
        {
            get; set;
        }

        /// <inheritdoc/>
        [JsonPropertyName("codename")]
        public string Codename
        {
            get; internal set;
        }

        /// <inheritdoc/>
        [JsonPropertyName("url_slug")]
        public string UrlSlug
        {
            get; internal set;
        }

        /// <inheritdoc/>
        [JsonPropertyName("type")]
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
