namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Specifies the sort order direction for query results.
/// </summary>
public enum OrderingMode
{
    /// <summary>
    /// Sort results in ascending order (A-Z, 0-9, oldest to newest).
    /// </summary>
    Ascending,

    /// <summary>
    /// Sort results in descending order (Z-A, 9-0, newest to oldest).
    /// </summary>
    Descending,
}
