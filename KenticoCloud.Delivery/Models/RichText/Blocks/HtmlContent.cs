using System.ComponentModel.DataAnnotations;

namespace KenticoKontent.Delivery
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
