using System;
using System.ComponentModel.DataAnnotations;

namespace Kentico.Kontent.Delivery.ContentItems.RichText.Attributes
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
