using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.Elements;

internal class DateTimeElementValue : ContentElementValue<DateTime?>, IDateTimeElementValue
{
    [JsonPropertyName("display_timezone")]
    public required string DisplayTimezone { get; set; }
}
