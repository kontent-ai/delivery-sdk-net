using System;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    public interface IContentItemSystemAttributes
    {
        string Codename { get; }
        string Id { get; }
        string Language { get; set; }
        DateTime LastModified { get; set; }
        string Name { get; }
        IReadOnlyList<string> SitemapLocation { get; }
        string Type { get; }
    }
}