﻿using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.ContentLinks;

namespace Kentico.Kontent.Delivery.ContentItems.ContentLinks
{
    /// <inheritdoc/>
    [DebuggerDisplay("Codename = {" + nameof(IContentLink.Codename) + "}")]
    internal sealed class ContentLink : IContentLink
    {
        /// <inheritdoc/>
        Guid IContentLink.Id
        {
            get; set;
        }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename
        {
            get; internal set;
        }

        /// <inheritdoc/>
        [JsonProperty("url_slug")]
        public string UrlSlug
        {
            get; internal set;
        }

        /// <inheritdoc/>
        [JsonProperty("type")]
        public string ContentTypeCodename
        {
            get; internal set;
        }

        [JsonConstructor]
        public ContentLink()
        {
        }
    }
}
