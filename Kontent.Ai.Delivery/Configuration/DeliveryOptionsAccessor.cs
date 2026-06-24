using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.Configuration;

/// <summary>
/// Provides the currently effective <see cref="DeliveryOptions"/> for a client,
/// decoupling consumers from how options are sourced (currently a named
/// <see cref="IOptionsMonitor{TOptions}"/> registration).
/// </summary>
internal interface IDeliveryOptionsAccessor
{
    DeliveryOptions Current { get; }
}

/// <summary>
/// Reads options live from an <see cref="IOptionsMonitor{TOptions}"/> under a named registration.
/// Used by DI-registered clients so runtime options changes propagate.
/// </summary>
internal sealed class MonitorOptionsAccessor : IDeliveryOptionsAccessor
{
    private readonly IOptionsMonitor<DeliveryOptions> _monitor;
    private readonly string _name;

    public MonitorOptionsAccessor(IOptionsMonitor<DeliveryOptions> monitor, string name)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _monitor = monitor;
        _name = name;
    }

    public DeliveryOptions Current => _monitor.Get(_name);
}
