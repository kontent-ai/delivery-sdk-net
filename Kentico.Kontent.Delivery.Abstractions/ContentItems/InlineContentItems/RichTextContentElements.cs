namespace Kentico.Kontent.Delivery.Abstractions.ContentItems.InlineContentItems
{
    /// <summary>
    /// Structure holding identifier of specific richtext element used in content item.
    /// </summary>
    internal readonly struct RichTextContentElements
    {
        public string ContentItemCodeName { get; }

        public string RichTextElementCodeName { get; }

        public RichTextContentElements(string contentItemCodeName, string richTextElementCodeName)
        {
            RichTextElementCodeName = richTextElementCodeName;
            ContentItemCodeName = contentItemCodeName;
        }
    }
}