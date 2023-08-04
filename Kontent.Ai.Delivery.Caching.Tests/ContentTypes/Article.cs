using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.Attributes;

namespace Kontent.Ai.Delivery.Caching.Tests.ContentTypes
{
    public partial class Article
    {
        [PropertyName("post_date")]
        public IDateTimeContent PostDateContent { get; set; }
    }
}