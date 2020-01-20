using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.Abstractions.ContentLinks
{
    public interface IContentLinkResolver
    {
        IContentLinkUrlResolver ContentLinkUrlResolver { get; }
        string ResolveContentLinks(string text, JToken links);
    }
}