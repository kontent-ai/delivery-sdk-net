using System;
using System.Collections.Generic;
using System.Diagnostics;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <inheritdoc/>
    [DebuggerDisplay("Id = {" + nameof(Id) + "}")]
    public sealed class ContentItemSystemAttributes : IContentItemSystemAttributes
    {
        /// <inheritdoc/>
        [JsonProperty("id")]
        public string Id { get; }

        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name { get; }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename { get; }

        /// <inheritdoc/>
        [JsonProperty("type")]
        public string Type { get; }

        /// <inheritdoc/>
        [JsonProperty("sitemap_locations")]
        public IReadOnlyList<string> SitemapLocation { get; }

        /// <inheritdoc/>
        [JsonProperty("last_modified")]
        public DateTime LastModified { get; set; }

        /// <inheritdoc/>
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
            LastModified = lastModified.ToUniversalTime();
            Language = language;
        }
    }
}