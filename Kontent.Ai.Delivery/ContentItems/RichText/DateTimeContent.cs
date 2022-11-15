using System;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentItems.RichText
{
    internal class DateTimeContent : IDateTimeContent
    {
        [JsonConstructor]
        public DateTimeContent()
        {
        }

        public DateTime? Value {get; set;}
        public string DisplayTimezone {get; set;}
    }
}
