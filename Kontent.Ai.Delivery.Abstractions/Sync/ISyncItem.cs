using System;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a delta update.
/// </summary>
public interface ISyncItem
{
    /// <summary>
    /// Gets the content item's codename.
    /// </summary>
    string Codename { get; }
    
    /// <summary>
    /// Gets the content item's internal ID.
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// Gets the content item's type codename.
    /// </summary>
    string Type { get; }
    
    /// <summary>
    /// Gets the codename of the language that the content is in.
    /// </summary>
    string Language { get; }
    
    /// <summary>
    /// Gets the content item's collection codename. For projects without collections enabled, the value is default.
    /// </summary>
    string Collection { get; }
    
    /// <summary>
    /// Gets the information whether the content item was modified or deleted since the last synchronization.
    /// </summary>
    string ChangeType { get; }
    
    /// <summary>
    /// Gets the ISO-8601 formatted date and time in UTC of the last change to the content item. The timestamp identifies when the change occurred in Delivery API.
    /// </summary>
    DateTime Timestamp { get; }
}