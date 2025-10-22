using System.Collections.Concurrent;

namespace Kontent.Ai.Delivery;

/// <summary>
/// Internal registry to track registered DeliveryClient names and ensure uniqueness.
/// Uses ConcurrentDictionary for lock-free thread-safe operations.
/// </summary>
internal sealed class DeliveryClientRegistry
{
    private readonly ConcurrentDictionary<string, byte> _registeredNames = new();

    /// <summary>
    /// Attempts to register a client name.
    /// </summary>
    /// <param name="name">The client name to register.</param>
    /// <returns>True if the name was successfully registered; false if it was already registered.</returns>
    public bool TryRegister(string name) => _registeredNames.TryAdd(name, 0);
}