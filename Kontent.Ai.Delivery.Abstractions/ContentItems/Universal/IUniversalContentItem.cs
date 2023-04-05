using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Dynamic representation of the content item dynamic item processing.
    /// Values are based on <see cref="Kontent.Ai.Delivery.Abstractions.IContentElementValue{T}"/>
    /// </summary>
    public interface IUniversalContentItem : IContentItem
    {
        /// <summary>
        /// Represents content items elements.
        /// </summary>
        public Dictionary<string, IContentElementValue> Elements { get; set; }
    }
}
