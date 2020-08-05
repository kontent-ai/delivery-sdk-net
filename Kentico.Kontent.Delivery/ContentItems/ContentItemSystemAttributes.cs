using System;
using System.Collections.Generic;
using System.Diagnostics;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <inheritdoc/>
    [DebuggerDisplay("Id = {" + nameof(Id) + "}")]
    internal sealed class ContentItemSystemAttributes : IContentItemSystemAttributes
    {
        /// <inheritdoc/>
        [JsonProperty("id")]
        public string Id { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("type")]
        public string Type { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("sitemap_locations")]
        public IReadOnlyList<string> SitemapLocation { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("last_modified")]
        public DateTime LastModified { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("language")]
        public string Language { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentItemSystemAttributes"/> class.
        /// </summary>
        [JsonConstructor]
        public ContentItemSystemAttributes()
        {
        }
    }
}