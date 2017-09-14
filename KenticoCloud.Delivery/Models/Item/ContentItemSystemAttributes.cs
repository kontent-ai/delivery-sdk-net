using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents system attributes of a content item.
    /// </summary>
    public sealed class ContentItemSystemAttributes
    {
        /// <summary>
        /// Gets the identifier of the content item.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; }

        /// <summary>
        /// Gets the name of the content item.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; }

        /// <summary>
        /// Gets the codename of the content item.
        /// </summary>
        [JsonProperty("codename")]
        public string Codename { get; }

        /// <summary>
        /// Gets the codename of the content type, for example "article".
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; }

        /// <summary>
        /// Gets a list of codenames of sitemap items to which the content item is assigned.
        /// </summary>
        [JsonProperty("sitemap_locations")]
        public IReadOnlyList<string> SitemapLocation { get; }

        /// <summary>
        /// Gets the time the content item was last modified.
        /// </summary>
        [JsonProperty("last_modified")]
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Gets the language of the content item.
        /// </summary>
        [JsonProperty("language")]
        public string Language { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentItemSystemAttributes"/> class.
        /// </summary>
        [JsonConstructor]
        internal ContentItemSystemAttributes(string id, string name, string codename, string type, IReadOnlyList<string> sitemapLocation, DateTime lastModified, string language)
        {
            Id = id;
            Name = name;
            Codename = codename;
            Type = type;
            SitemapLocation = sitemapLocation;
            LastModified = lastModified;
            Language = language;
        }
    }
}
