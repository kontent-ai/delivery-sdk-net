using Kontent.Ai.Delivery.ContentItems.RichText.Attributes;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

/// <inheritdoc cref="IInlineImage" />
[DisableHtmlEncode]
[UseDisplayTemplate("InlineImage")]
[DebuggerDisplay("Url = {" + nameof(Url) + "}")]
[method: JsonConstructor]
internal class InlineImage() : IInlineImage
{
    public string Description { get; set; }

    public string Url { get; set; }

    public int Height { get; set; }

    public int Width { get; set; }

    public Guid ImageId { get; set; }

    public override string ToString()
    {
        return $"<figure><img src=\"{Url}\" alt=\"{Description}\"></figure>";
    }
}
