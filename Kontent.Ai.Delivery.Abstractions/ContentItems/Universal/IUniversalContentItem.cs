using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions
{
    // TODO validate this to get whole element context
    /// <summary>
    /// Dynamic representation of the content item dynamic item processing.
    /// Values are based on <see cref="Kontent.Ai.Delivery.Abstractions.IContentElementValue{T}"/>
    /// </summary>
    public interface IUniversalContentItem : IContentItem
    {
        // TODO 
        public Dictionary<string, IContentElementValue> Elements { get; set; }
    }
}
