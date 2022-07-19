using System;
using System.ComponentModel.DataAnnotations;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Attributes
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
