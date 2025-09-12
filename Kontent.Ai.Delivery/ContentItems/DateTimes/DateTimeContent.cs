using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.DateTimes;

[method: JsonConstructor]
internal sealed class DateTimeContent() : IDateTimeContent
{
    [JsonPropertyName("value")]
    public DateTime? Value
    {
        get; internal set;
    }

    [JsonPropertyName("display_timezone")]
    public string DisplayTimezone
    {
        get; internal set;
    }
}

