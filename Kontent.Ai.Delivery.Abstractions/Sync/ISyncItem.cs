using System;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a delta update.
/// </summary>
public interface ISyncItem
{
    /// <summary>
    /// Retrieves runtime strongly typed item if CustomTypeProvider is registered, otherwise null.
    /// </summary>
    object StronglyTypedData { get; }

    /// <summary>
    /// Retrieves content item information and element values.
    /// </summary>
    ISyncItemData Data { get; }
    
    /// <summary>
    /// Gets the information whether the content item was modified or deleted since the last synchronization.
    /// </summary>
    string ChangeType { get; }
    
    /// <summary>
    /// Gets the ISO-8601 formatted date and time in UTC of the last change to the content item. The timestamp identifies when the change occurred in Delivery API.
    /// </summary>
    DateTime Timestamp { get; }
}