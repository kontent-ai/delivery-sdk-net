using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;
using System;

namespace Kontent.Ai.Delivery.ContentItems.Elements
{
    internal class DateTimeElementValue : ContentElementValue<DateTime>, IDateTimeElementValue
    {
        [JsonProperty("display_timezone")]
        public string DisplayTimezone { get; set; }
    }
}
