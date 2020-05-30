using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    public interface ITaxonomyTermDetails
    {
        string Codename { get; }
        string Name { get; }
        IReadOnlyList<ITaxonomyTermDetails> Terms { get; }
    }
}