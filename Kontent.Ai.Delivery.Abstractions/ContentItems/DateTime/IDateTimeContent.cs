using System;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents DateTimeElement content in a form of structured data 
    /// </summary>
    public interface IDateTimeContent
    {
        /// <summary>
        /// Gets the value of DateTime element
        /// </summary>
        public DateTime? Value { get; }

        /// <summary>
        /// Gets the Timezone of DateTime element
        /// </summary>
        public string DisplayTimezone { get; }
    }
}
