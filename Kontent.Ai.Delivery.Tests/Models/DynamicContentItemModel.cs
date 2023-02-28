using Kontent.Ai.Delivery.Abstractions;
namespace Kontent.Ai.Delivery.Tests.Models
{
    public class DynamicContentItemModel
    {
        public IContentItemSystemAttributes System { get; set; }

        public ContentItemElements Elements { get; set; }
    }
}