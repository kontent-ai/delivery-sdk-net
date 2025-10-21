using System;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Context of the current resolving process
/// </summary>
public class ResolvingContext
{
    /// <summary>
    /// Gets the content item within current resolving context
    /// </summary>
    public required Func<string, Task<object>> GetLinkedItem { get; init; }
}