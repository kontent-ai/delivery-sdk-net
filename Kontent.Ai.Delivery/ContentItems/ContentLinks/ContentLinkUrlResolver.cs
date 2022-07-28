using Kontent.Ai.Delivery.Abstractions;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.ContentItems.ContentLinks
{
    internal class DefaultContentLinkUrlResolver : IContentLinkUrlResolver
    {
        public Task<string> ResolveBrokenLinkUrlAsync()
            => Task.FromResult<string>(null);

        public Task<string> ResolveLinkUrlAsync(IContentLink link)
            => Task.FromResult<string>(null);
    }
}
