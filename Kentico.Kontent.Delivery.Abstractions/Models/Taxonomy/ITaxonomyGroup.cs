using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    public interface ITaxonomyGroup
    {
        ITaxonomyGroupSystemAttributes System { get; }
        IReadOnlyList<ITaxonomyTermDetails> Terms { get; }
    }
}