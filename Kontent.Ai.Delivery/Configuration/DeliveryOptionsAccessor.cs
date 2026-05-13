using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.Configuration;

/// <summary>
/// Provides the currently effective <see cref="DeliveryOptions"/> for a client.
/// Hides the question of whether values come from <see cref="IOptionsMonitor{TOptions}"/>
/// (DI-driven, reactive) or a captured snapshot (standalone construction).
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

/// <summary>
/// Returns a fixed <see cref="DeliveryOptions"/> snapshot. Used by clients constructed
/// outside DI, where the caller has handed the SDK a pre-built options instance and does
/// not expect runtime reactivity.
/// </summary>
internal sealed class SnapshotOptionsAccessor(DeliveryOptions snapshot) : IDeliveryOptionsAccessor
{
    private readonly DeliveryOptions _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));

    public DeliveryOptions Current => _snapshot;
}
