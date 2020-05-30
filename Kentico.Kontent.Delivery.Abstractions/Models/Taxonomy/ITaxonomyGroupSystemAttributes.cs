using System;

namespace Kentico.Kontent.Delivery.Abstractions
{
    public interface ITaxonomyGroupSystemAttributes
    {
        string Codename { get; }
        string Id { get; }
        DateTime LastModified { get; }
        string Name { get; }
    }
}