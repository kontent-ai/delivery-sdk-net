using System;
using System.Reflection;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Specifies property of a model to map DisplayTimezone value into
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DisplayTimzoneConverterAttribute : Attribute, IPropertyValueConverter<DateTime>
    {
        Task<object> IPropertyValueConverter<DateTime>.GetPropertyValueAsync<TElement>(PropertyInfo property, TElement element, ResolvingContext context)
        {
            if (element is not IDateTimeElementValue dateTimeElement)
            {
                return null;
            }

            return Task.FromResult<object>(dateTimeElement.DisplayTimezone);
        }
    }
}
