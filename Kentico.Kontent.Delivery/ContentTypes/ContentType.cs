﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentTypes;
using Kentico.Kontent.Delivery.Abstractions.ContentTypes.Element;
using Kentico.Kontent.Delivery.ContentTypes.Element;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentTypes
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(System) + "." + nameof(IContentTypeSystemAttributes.Name) + "}")]
    internal sealed class ContentType : IContentType
    {
        /// <inheritdoc/>
        [JsonProperty("system")]
        public IContentTypeSystemAttributes System { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("elements")]
        public IDictionary<string, IContentElement> Elements { get; internal set; }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        [JsonConstructor]
        public ContentType(IContentTypeSystemAttributes system, IDictionary<string, IContentElement> elements)
        {
            System = system;
            Elements = elements;

            // Initialize codenames
            foreach (var element in Elements.Where(r => r.Value is ContentElement).Select(a => (Codename: a.Key, Element: (ContentElement)a.Value)))
            {
                element.Element.Codename = element.Codename;
            }
        }
    }
}
