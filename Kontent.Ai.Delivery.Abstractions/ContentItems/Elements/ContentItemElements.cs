using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions
{
    // TODO validate this to get whole element context
    /// <summary>
    /// Dynamic dictionary of strongly type elements for dynamic item fetch.
    /// Values are based on <see cref="Kontent.Ai.Delivery.Abstractions.IContentElementValue{T}"/>
    /// </summary>
    public class ContentItemElements : Dictionary<string, IContentElementValue<object>>
    {

    }
}