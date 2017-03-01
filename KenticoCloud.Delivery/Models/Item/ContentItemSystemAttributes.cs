using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

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
        public string Id { get; }

        /// <summary>
        /// Gets the name of the content item.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the codename of the content item.
        /// </summary>
        public string Codename { get; }

        /// <summary>
        /// Gets the codename of the content type, for example "article".
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Gets a list of codenames of sitemap items to which the content item is assigned.
        /// </summary>
        public IReadOnlyList<string> SitemapLocation { get; }

        /// <summary>
        /// Gets the time the content item was last modified.
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentItemSystemAttributes"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        internal ContentItemSystemAttributes(JToken source)
            : this(source["id"].ToString(), source["name"].ToString(), source["codename"].ToString(), source["type"].ToString(), ((JArray)source["sitemap_locations"]).Values<string>().ToList().AsReadOnly(), source["last_modified"].Value<DateTime>())
        {
        }

        [JsonConstructor]
        internal ContentItemSystemAttributes(string id, string name, string codename, string type, IReadOnlyList<string> sitemapLocation, DateTime lastModified)
        {
            Id = id;
            Name = name;
            Codename = codename;
            Type = type;
            SitemapLocation = sitemapLocation;
            LastModified = lastModified;
        }
    }
}
