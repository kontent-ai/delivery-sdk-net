using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;
using System;

namespace Kontent.Ai.Delivery.ContentItems.Elements
{
    public class DateTimeElementValue : ContentElementValue<DateTime?>, IDateTimeElementValue
    {
        [JsonProperty("display_timezone")]
        public string DisplayTimezone { get; set; }
    }
}
