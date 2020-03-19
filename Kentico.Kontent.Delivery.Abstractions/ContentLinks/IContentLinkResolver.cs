using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.Abstractions.ContentLinks
{
     /// <summary>
     /// Resolves links in rich text elements.
     /// </summary>
    public interface IContentLinkResolver
    {
        /// <summary>
        /// Resolver for url links in rich text elements.
        /// </summary>
        IContentLinkUrlResolver ContentLinkUrlResolver { get; }

        /// <summary>
        /// Get whole link in tich text elements
        /// </summary>
        /// <param name="text">Text which you want resolve as link</param>
        /// <param name="links">Element links</param>
        /// <returns></returns>
        string ResolveContentLinks(string text, JToken links);
    }
}