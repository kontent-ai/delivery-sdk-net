using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.Attributes;

namespace Kontent.Ai.Delivery.Caching.Tests
{
    public class TestItem
    {
        [PropertyName("property_name_does_not_affect_caching")]
        public string Title { get; set; }
        public IContentItemSystemAttributes System { get; set; }
    }
}
