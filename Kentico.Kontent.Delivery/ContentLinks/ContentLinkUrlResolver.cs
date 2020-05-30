using Kentico.Kontent.Delivery.Abstractions.ContentLinks;

namespace Kentico.Kontent.Delivery.ContentLinks
{
    internal class DefaultContentLinkUrlResolver : IContentLinkUrlResolver
    {
        public string ResolveLinkUrl(IContentLink link) 
            => null;

        public string ResolveBrokenLinkUrl()
            => null;
    }
}
