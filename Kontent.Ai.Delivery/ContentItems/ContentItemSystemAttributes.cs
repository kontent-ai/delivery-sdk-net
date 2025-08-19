using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems
{
    /// <inheritdoc/>
    [DebuggerDisplay("Id = {" + nameof(Id) + "}")]
    internal sealed class ContentItemSystemAttributes : IContentItemSystemAttributes
    {
        /// <inheritdoc/>
        [JsonPropertyName("id")]
        public string Id { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("name")]
        public string Name { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("codename")]
        public string Codename { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("type")]
        public string Type { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("sitemap_locations")]
        public IList<string> SitemapLocation { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("last_modified")]
        public DateTime LastModified { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("language")]
        public string Language { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("collection")]
        public string Collection { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("workflow")]
        public string Workflow { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("workflow_step")]
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