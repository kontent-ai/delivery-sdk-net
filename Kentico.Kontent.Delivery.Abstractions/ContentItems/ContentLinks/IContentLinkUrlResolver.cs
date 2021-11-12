using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery.Abstractions.ContentItems.ContentLinks
{
    /// <summary>
    /// Defines the contract to resolve content links in Rich text element values.
    /// </summary>
    public interface IContentLinkUrlResolver
    {
        /// <summary>
        /// Returns a URL of the linked content item.
        /// </summary>
        /// <param name="link">The link to a content item that needs to be resolved.</param>
        /// <returns>The URL of the linked content item, if possible; otherwise, <c>null</c>.</returns>
        Task<string> ResolveLinkUrlAsync(IContentLink link);

        /// <summary>
        /// Returns a URL of the linked content item that is not available.
        /// </summary>
        /// <returns>The URL of the linked content item that is not available, if possible; otherwise, <c>null</c>.</returns>
        Task<string> ResolveBrokenLinkUrlAsync();
    }
}
