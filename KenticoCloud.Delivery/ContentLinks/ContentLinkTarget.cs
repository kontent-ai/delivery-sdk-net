using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    // Represents a linked content item.
    public sealed class ContentLinkTarget
    {
        /// <summary>
        /// Gets the identifier of the linked content item.
        /// </summary>
        public string Id
        {
            get;
        }

        /// <summary>
        /// Gets the codename of the linked content item.
        /// </summary>
        public string Codename
        {
            get;
        }

        /// <summary>
        /// Gets the URL slug of the linked content item, if available; otherwise, <c>null</c>.
        /// </summary>
        public string UrlSlug
        {
            get;
        }

        /// <summary>
        /// Gets the content type codename of the linked content item.
        /// </summary>
        public string ContentTypeCodename
        {
            get;
        }

        internal ContentLinkTarget(string id, JToken source)
        {
            Id = id;
            Codename = source.Value<string>("codename");
            UrlSlug = source.Value<string>("url_slug");
            ContentTypeCodename = source.Value<string>("type");
        }
    }
}
