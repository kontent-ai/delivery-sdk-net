using FluentAssertions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.DateTimes;
using Kontent.Ai.Delivery.ContentItems.Elements;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.ValueConverters
{
    public class DateTimeContentConverterTests
    {
        private readonly DateTimeContentConverter _converter;

        public DateTimeContentConverterTests()
        {
            _converter = new DateTimeContentConverter();
        }

        [Fact]
        public async Task GetPropertyValue_IncorrectModelPropertyType_ThrowsException()
        {
            var property = typeof(TestModel).GetProperty("WrongDateTimeContent");
            var contentElement = new DateTimeElementValue
            {
                Type = "date_time",
                Name = "Some name",
                Codename = "some_codename",
                Value = DateTime.UtcNow,
                DisplayTimezone = "Europe/Prague",
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await _converter.GetPropertyValueAsync(property, contentElement, null));
        }

        [Fact]
        public async Task GetPropertyValue_IncorrectContentElement_ReturnsNull()
        {
            var property = typeof(TestModel).GetProperty("CorrectDateTimeContent");
            var contentElement = new WrongDateTimeElementValue
            {
                Type = "some_type",
                Name = "Some name",
                Codename = "some_codename",
                Value = DateTime.Now,
            };

            var convertedPropertyValue = await _converter.GetPropertyValueAsync(property, contentElement, null);

            convertedPropertyValue.Should().BeNull();
        }

        [Fact]
        public async Task GetPropertyValue_ConvertsCorrectly()
        {
            var expected = new DateTimeContent
            {
                Value = DateTime.UtcNow,
                DisplayTimezone = "Europe/Prague"
            };

            var property = typeof(TestModel).GetProperty("CorrectDateTimeContent");

            var contentElement = new DateTimeElementValue
            {
                Type = "date_time",
                Name = "Some name",
                Codename = "some_codename",
                Value = expected.Value,
                DisplayTimezone = expected.DisplayTimezone,
            };

            var convertedPropertyValue = await _converter.GetPropertyValueAsync(property, contentElement, null);

            convertedPropertyValue.Should().BeEquivalentTo(expected);
        }

        private class TestModel
        {
            public DateTime? WrongDateTimeContent { get; set; }
            public DateTimeContent CorrectDateTimeContent { get; set; }
        }

        private class WrongDateTimeElementValue : ContentElementValue<DateTime?> { }
    }
}
