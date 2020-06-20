using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentItems.RichText.Attributes;
using System.Diagnostics;

namespace Kentico.Kontent.Delivery.ContentItems.RichText.Blocks
{
    [DisableHtmlEncode]
    [UseDisplayTemplate("InlineImage")]
    [DebuggerDisplay("Url = {" + nameof(Url) + "}")]
    internal class InlineImage : IInlineImage
    {
        public string Description { get; set; }

        public string Url { get; set; }

        public int Height { get; set; }

        public int Width { get; set; }

        public override string ToString()
        {
            return $"<figure><img src=\"{Url}\" alt=\"{Description}\"></figure>";
        }
    }
}
