using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Extensions.Universal
{
    public class UniversalContentItem : IUniversalContentItem
    {
        public IContentItemSystemAttributes System { get; set; }
        public Dictionary<string, IContentElementValue> Elements { get; set; } = new Dictionary<string, IContentElementValue>();
    }
}