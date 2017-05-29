namespace KenticoCloud.Delivery.ContentItemsInRichText
{
    public interface IContentItemsInRichTextResolver
    {
        
    }

    public interface IContentItemsInRichTextResolver<T> : IContentItemsInRichTextResolver
    {
        string Resolve(ResolvedContentItemWrapper<T> wrapper);
    }
}
