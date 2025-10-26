using Kontent.Ai.Delivery.ContentItems.RichText.Attributes;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

/// <inheritdoc cref="IInlineImage" />
[DisableHtmlEncode]
[UseDisplayTemplate("InlineImage")]
[DebuggerDisplay("Url = {" + nameof(Url) + "}")]
internal sealed record InlineImage(
    string? Description,
    string Url,
    int Height,
    int Width,
    Guid ImageId
) : IInlineImage
{
}