using Kontent.Ai.Delivery.ContentItems.Elements;

namespace Kontent.Ai.Delivery.Extensions
{
    internal static class IContentElementValueExtensions
    {
        internal static ContentElementValue<T> WithCodename<T>(this ContentElementValue<T> elementValue, string codename)
        {
            elementValue.Codename = codename;
            return elementValue;
        }
    }
}