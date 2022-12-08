using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;
using System;

namespace Kontent.Ai.Delivery.ContentItems.DateTimes
{
    internal sealed class DateTimeContent : IDateTimeContent
    {
        public DateTime? Value 
        { 
            get; internal set;
        }

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
