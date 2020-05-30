using Kentico.Kontent.Delivery.Abstractions.ContentLinks;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.ContentLinks
{
    /// <inheritdoc/>
    public sealed class ContentLink : IContentLink
    {
        /// <inheritdoc/>
        public string Id
        {
            get;
        }

        /// <inheritdoc/>
        public string Codename
        {
            get;
        }

        /// <inheritdoc/>
        public string UrlSlug
        {
            get;
        }

        /// <inheritdoc/>
        public string ContentTypeCodename
        {
            get;
        }

        internal ContentLink(string id, JToken source)
        {
            Id = id;
            Codename = source.Value<string>("codename");
            UrlSlug = source.Value<string>("url_slug");
            ContentTypeCodename = source.Value<string>("type");
        }
    }
}
