using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentItems.Attributes;

namespace Kentico.Kontent.Delivery.Caching.Tests
{
    public class TestItem
    {
        [PropertyName("property_name_does_not_affect_caching")]
        public string Title { get; set; }
        public IContentItemSystemAttributes System { get; set; }
    }
}
