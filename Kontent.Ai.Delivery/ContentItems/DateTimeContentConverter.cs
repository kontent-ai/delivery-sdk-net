using Kontent.Ai.Delivery.ContentItems.DateTimes;
using System.Reflection;

namespace Kontent.Ai.Delivery.ContentItems
{
    internal class DateTimeContentConverter : IPropertyValueConverter<DateTime?, IDateTimeContent>
    {
        public DateTimeContentConverter()
        {
        }

        public Task<IDateTimeContent?> GetPropertyValueAsync<TElement>(
            PropertyInfo property,
            TElement contentElement,
            ResolvingContext context
            ) where TElement : IContentElementValue<DateTime?>
        {
            if (!typeof(IDateTimeContent).IsAssignableFrom(property.PropertyType))
            {
                throw new InvalidOperationException($"Type of property {property.Name} must implement {nameof(IDateTimeContent)} in order to receive datetime content.");
            }

            if (contentElement is not IDateTimeElementValue element)
            {
                return Task.FromResult<IDateTimeContent?>(null);
            }

            var displayTimezone = element.DisplayTimezone;
            var value = element.Value;

            var dateTimeContent = new DateTimeContent
            {
                DisplayTimezone = displayTimezone,
                Value = value
            };

            return Task.FromResult<IDateTimeContent?>(dateTimeContent);
        }
    }
}
