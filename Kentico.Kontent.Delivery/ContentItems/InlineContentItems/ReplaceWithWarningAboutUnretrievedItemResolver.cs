using Kentico.Kontent.Delivery.Abstractions.ContentItems.InlineContentItems;

namespace Kentico.Kontent.Delivery.ContentItems.InlineContentItems
{
    /// <summary>
    /// Resolver which is replacing content items in richtext with warning message about insufficient depth for content item. Used as default for unretrieved content items resolver on Preview environment.
    /// </summary>
    internal class ReplaceWithWarningAboutUnretrievedItemResolver : IInlineContentItemsResolver<UnretrievedContentItem>
    {
        /// <inheritdoc />
        public string Resolve(UnretrievedContentItem item)
            => "This inline content item was not resolved because it was not retrieved from Delivery API.";
    }
}