using System;
namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a delta update.
/// </summary>
public interface ISyncV2Language
{
    /// <summary>
    /// Retrieves taxonomy information.
    /// </summary>
    ISyncV2LanguageData Data { get; }

    /// <summary>
    /// Gets the information whether the taxonomy was modified or deleted since the last synchronization.
    /// </summary>
    string ChangeType { get; }

    /// <summary>
    /// Gets the ISO-8601 formatted date and time in UTC of the last change to the taxonomy. The timestamp identifies when the change occurred in Delivery API.
    /// </summary>
    DateTime Timestamp { get; }
}