using System.ComponentModel.DataAnnotations;
using Kentico.Kontent.Delivery.Abstractions.Models.RichText.Blocks;
using Kentico.Kontent.Delivery.StrongTyping.RichText.Attributes;

namespace Kentico.Kontent.Delivery.StrongTyping.RichText.Blocks
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
