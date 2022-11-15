using System;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents DateTimeElement content in a form of structured data 
    /// </summary>
    public interface IDateTimeContent
    {
        public DateTime? Value { get; set; }
        public string DisplayTimezone { get; set; }
    }
}
