using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentItems;

namespace Kentico.Kontent.Delivery.Caching.Tests
{
    public class TestItem
    {
        public string Title { get; set; }
        public IContentItemSystemAttributes System { get; set; }
    }
}
