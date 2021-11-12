using Kentico.Kontent.Delivery.Abstractions;
using System.Threading.Tasks;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.ContentLinks;

namespace Kentico.Kontent.Delivery.ContentItems.ContentLinks
{
    internal class DefaultContentLinkUrlResolver : IContentLinkUrlResolver
    {
        public Task<string> ResolveBrokenLinkUrlAsync()
            => Task.FromResult<string>(null);

        public Task<string> ResolveLinkUrlAsync(IContentLink link)
            => Task.FromResult<string>(null);
    }
}
