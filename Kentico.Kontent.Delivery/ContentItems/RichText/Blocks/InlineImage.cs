using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentItems.RichText.Attributes;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.RichText.Blocks;

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

        public Guid ImageId { get; set; }

        [JsonConstructor]
        public InlineImage()
        {
        }

        public override string ToString()
        {
            return $"<figure><img src=\"{Url}\" alt=\"{Description}\"></figure>";
        }
    }
}
