using System.ComponentModel.DataAnnotations;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Attributes;

[AttributeUsage(AttributeTargets.Class)]
class UseDisplayTemplateAttribute(string uiHint) : UIHintAttribute(uiHint)
{
}