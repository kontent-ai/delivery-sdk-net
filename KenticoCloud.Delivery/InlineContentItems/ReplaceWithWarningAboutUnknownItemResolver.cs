namespace KenticoCloud.Delivery.InlineContentItems
{
    internal class ReplaceWithWarningAboutUnknownItemResolver : IInlineContentItemsResolver<UnknownContentItem>
    {
        public string Resolve(ResolvedContentItemData<UnknownContentItem> item)
            => $"Content type '{item.Item.Type}' has no corresponding model.";
    }
}
