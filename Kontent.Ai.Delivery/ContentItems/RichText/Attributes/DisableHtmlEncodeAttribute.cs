using System;
using System.ComponentModel.DataAnnotations;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Attributes
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
