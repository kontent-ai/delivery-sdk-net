using System.ComponentModel.DataAnnotations;
using Kontent.Ai.Delivery.ContentItems.RichText.Attributes;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

[DisableHtmlEncode]
internal class HtmlContent : IHtmlContent
{
    [DataType(DataType.Html)]
    public required string Html { get; set; }

    public override string ToString()
    {
        return Html;
    }
}
