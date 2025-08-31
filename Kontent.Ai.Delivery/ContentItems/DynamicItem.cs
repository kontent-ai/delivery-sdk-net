using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems
{
    internal sealed class DynamicItem
    {
        public IContentItemSystemAttributes System { get; }
        public IReadOnlyDictionary<string, JsonElement> Elements { get; }

        public DynamicItem(IContentItemSystemAttributes system, IReadOnlyDictionary<string, JsonElement> elements)
        {
            System = system;
            Elements = elements;
        }
    }
}


