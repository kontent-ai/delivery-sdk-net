using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes
{
    public record Cafe
    (
        [property: JsonPropertyName("city")]
        string City,
        
        [property: JsonPropertyName("country")]
        string Country,
        
        [property: JsonPropertyName("email")]
        string Email,
        
        [property: JsonPropertyName("phone")]
        string Phone,
        
        [property: JsonPropertyName("photo")]
        IEnumerable<IAsset> Photo,
        
        [property: JsonPropertyName("sitemap")]
        IEnumerable<ITaxonomyTerm> Sitemap,
        
        [property: JsonPropertyName("state")]
        string State,
        
        [property: JsonPropertyName("street")]
        string Street,
        
        [property: JsonPropertyName("zip_code")]
        string ZipCode
    );
}