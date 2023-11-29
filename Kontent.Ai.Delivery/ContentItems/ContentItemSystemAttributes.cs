using System;
using System.Collections.Generic;
using System.Diagnostics;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentItems
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
        public IList<string> SitemapLocation { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("last_modified")]
        public DateTime LastModified { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("language")]
        public string Language { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("collection")]
        public string Collection { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("workflow")]
        public string Workflow { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("workflow_step")]
        public string WorkflowStep { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentItemSystemAttributes"/> class.
        /// </summary>
        [JsonConstructor]
        public ContentItemSystemAttributes()
        {
        }
    }
}