using System.ComponentModel.DataAnnotations;

namespace Kentico.Kontent.Delivery.Abstractions
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
