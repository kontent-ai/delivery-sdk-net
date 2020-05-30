using System;
using System.ComponentModel.DataAnnotations;

namespace Kentico.Kontent.Delivery.StrongTyping.RichText.Attributes
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
