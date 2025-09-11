using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes
{
    public record HeroUnit
    (
        [property: JsonPropertyName("image")]
        IEnumerable<IAsset> Image,
        
        [property: JsonPropertyName("marketing_message")]
        string MarketingMessage,
        
        [property: JsonPropertyName("sitemap")]
        IEnumerable<ITaxonomyTerm> Sitemap,
        
        [property: JsonPropertyName("title")]
        string Title
    );
}