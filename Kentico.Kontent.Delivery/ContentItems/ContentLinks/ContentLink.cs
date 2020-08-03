using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        [JsonProperty("codename")]
        /// <inheritdoc/>
        public string Codename
        {
            get; internal set;
        }

        [JsonProperty("url_slug")]
        /// <inheritdoc/>
        public string UrlSlug
        {
            get; internal set;
        }

        [JsonProperty("type")]
        /// <inheritdoc/>
        public string ContentTypeCodename
        {
            get; internal set;
        }

        public ContentLink(string id, JToken source)
        {
            //TODO: reduce constructors
            Id = id;
            Codename = source.Value<string>("codename");
            UrlSlug = source.Value<string>("url_slug");
            ContentTypeCodename = source.Value<string>("type");
        }


        [JsonConstructor]
        public ContentLink()
        {
        }


        public ContentLink(string codename, string type, string urlSlug)
        {
            Codename = codename;
            ContentTypeCodename = type;
            UrlSlug = urlSlug;
        }
    }
}
