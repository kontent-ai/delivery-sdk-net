using System;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// An element representing a DateTimeElement value. In addition to datetime timestamp, 
    /// the date time element's value property contains string representing timezone set in the UI.
    /// </summary>
    public interface IDateTimeElementValue : IContentElementValue<DateTime>
    {
        /// <summary>
        /// The specified timezone.
        /// </summary>
        string DisplayTimezone { get; }
    }
}
