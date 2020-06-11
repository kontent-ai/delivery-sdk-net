using Kentico.Kontent.Delivery.Abstractions;

namespace Kentico.Kontent.Delivery.ContentItems.ContentLinks
{
    internal class DefaultContentLinkUrlResolver : IContentLinkUrlResolver
    {
        public string ResolveLinkUrl(IContentLink link) 
            => null;

        public string ResolveBrokenLinkUrl()
            => null;
    }
}
