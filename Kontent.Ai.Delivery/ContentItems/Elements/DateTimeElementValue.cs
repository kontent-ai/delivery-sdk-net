using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentItems.Elements
{
    internal class DateTimeElementValue : ContentElementValue<DateTime?>, IDateTimeElementValue
    {
        [JsonProperty("display_timezone")]
        public required string DisplayTimezone { get; set; }
    }
}
