using System;
using System.ComponentModel.DataAnnotations;

namespace Kentico.Kontent.Delivery
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    class DisableHtmlEncodeAttribute : DisplayFormatAttribute
    {
        public DisableHtmlEncodeAttribute()
        {
            HtmlEncode = false;
        }
    }
}
