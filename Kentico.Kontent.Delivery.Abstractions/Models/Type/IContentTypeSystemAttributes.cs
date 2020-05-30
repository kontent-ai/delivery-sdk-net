using System;

namespace Kentico.Kontent.Delivery.Abstractions.Models.Type
{
    public interface IContentTypeSystemAttributes
    {
        string Codename { get; }
        string Id { get; }
        DateTime LastModified { get; }
        string Name { get; }
    }
}