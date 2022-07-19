using System;
using System.ComponentModel.DataAnnotations;

namespace Kentico.Kontent.Delivery.ContentItems.RichText.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    class UseDisplayTemplateAttribute : UIHintAttribute
    {
        public UseDisplayTemplateAttribute(string uiHint)
            : base(uiHint)
        {
        }
    }
}
