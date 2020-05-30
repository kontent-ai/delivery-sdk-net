using System;
using System.ComponentModel.DataAnnotations;

namespace Kentico.Kontent.Delivery.StrongTyping.RichText.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    class DisableHtmlEncodeAttribute : DisplayFormatAttribute
    {
        public DisableHtmlEncodeAttribute()
        {
            HtmlEncode = false;
        }
    }
}
