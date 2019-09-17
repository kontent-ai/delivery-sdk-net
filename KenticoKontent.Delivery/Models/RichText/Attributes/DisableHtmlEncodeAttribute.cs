using System;
using System.ComponentModel.DataAnnotations;

namespace KenticoKontent.Delivery
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
