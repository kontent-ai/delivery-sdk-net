using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentItems.DateTimes
{
    internal sealed class DateTimeContent : IDateTimeContent
    {
        [JsonProperty("value")]
        public DateTime? Value
        {
            get; internal set;
        }

        [JsonProperty("display_timezone")]
        public string DisplayTimezone
        {
            get; internal set;
        }

        [JsonConstructor]
        public DateTimeContent()
        {
        }
    }
}
