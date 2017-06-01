namespace KenticoCloud.Delivery.InlineContentItems
{
    /// <summary>
    /// Resolver which is replacing content items in richtext with warning message about insufficient depth for content item. Used as default for unretrieved content items resolver on Preview environment.
    /// </summary>
    public class ReplaceWithWarningAboutUnretrievedItemResolver : IInlineContentItemsResolver<UnretrievedContentItem>
    {
        /// <inheritdoc />
        public string Resolve(ResolvedContentItemData<UnretrievedContentItem> item)
        {
            return "This inline content item was not resolved because it was not retrieved from Delivery API.";
        }
    }
}