using System.Diagnostics;

namespace Kontent.Ai.Delivery.ContentItems.InlineContentItems;

/// <summary>
/// Structure holding identifier of specific richtext element used in content item.
/// </summary>
[DebuggerDisplay("Codename = {" + nameof(RichTextElementCodeName) + "}")]
internal readonly struct RichTextContentElements(string contentItemCodeName, string richTextElementCodeName)
{
    public string ContentItemCodeName { get; } = contentItemCodeName;

    public string RichTextElementCodeName { get; } = richTextElementCodeName;
}