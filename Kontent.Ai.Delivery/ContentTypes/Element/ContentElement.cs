﻿using System.Diagnostics;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentTypes.Element
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(Name) + "}")]
    internal class ContentElement : IContentElement
    {
        /// <inheritdoc/>
        [JsonProperty("type")]
        public string Type { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename { get; internal set; }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        [JsonConstructor]
        public ContentElement()
        {
        }
    }
}
