using System.ComponentModel.DataAnnotations;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentItems.RichText.Attributes;

namespace Kentico.Kontent.Delivery.ContentItems.RichText.Blocks
{
    [DisableHtmlEncode]
    internal class HtmlContent : IHtmlContent
    {
        [DataType(DataType.Html)]
        public string Html { get; set; }

        public override string ToString()
        {
            return Html;
        }
    }
}
