using Kentico.Kontent.Delivery.Abstractions.ContentItems.RichText.Blocks;
using Kentico.Kontent.Delivery.ContentItems.RichText.Attributes;

namespace Kentico.Kontent.Delivery.ContentItems.RichText.Blocks
{
    [DisableHtmlEncode]
    [UseDisplayTemplate("InlineImage")]
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
