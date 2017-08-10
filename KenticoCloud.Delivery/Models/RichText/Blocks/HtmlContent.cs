using System.ComponentModel.DataAnnotations;
using KenticoCloud.Delivery;

namespace KenticoCloud.Delivery
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
