using System;
using System.Diagnostics;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.UsedIn
{
    /// <inheritdoc/>
    [DebuggerDisplay("Id = {" + nameof(Id) + "}")]
    internal sealed class UsedInItemSystemAttributes : IUsedInItemSystemAttributes
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
        /// Initializes a new instance of the <see cref="UsedInItemSystemAttributes"/> class.
        /// </summary>
        [JsonConstructor]
        public UsedInItemSystemAttributes()
        {
        }
    }
}