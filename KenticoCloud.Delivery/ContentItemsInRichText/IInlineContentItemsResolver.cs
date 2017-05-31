namespace KenticoCloud.Delivery.ContentItemsInRichText
{
    public interface IContentItemsInRichTextResolver
    {
        
    }

    public interface IInlineContentItemsResolver<T> : IContentItemsInRichTextResolver
    {
        string Resolve(ResolvedContentItemData<T> data);
    }
}
