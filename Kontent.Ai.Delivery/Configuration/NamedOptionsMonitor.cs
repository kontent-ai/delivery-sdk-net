using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.Configuration;

/// <summary>
/// Wraps an <see cref="IOptionsMonitor{TOptions}"/> to always return a specific named option.
/// This enables reactive configuration updates while maintaining named option isolation.
/// </summary>
/// <typeparam name="TOptions">The options type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="NamedOptionsMonitor{TOptions}"/> class.
/// </remarks>
/// <param name="monitor">The underlying options monitor.</param>
/// <param name="name">The name of the options instance to retrieve.</param>
internal sealed class NamedOptionsMonitor<TOptions>(IOptionsMonitor<TOptions> monitor, string name) : IOptionsMonitor<TOptions>
    where TOptions : class
{
    private readonly IOptionsMonitor<TOptions> _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    private readonly string _name = name ?? throw new ArgumentNullException(nameof(name));

    /// <summary>
    /// Gets the current value of the named options instance.
    /// This property supports reactive updates when the underlying configuration changes.
    /// </summary>
    public TOptions CurrentValue => _monitor.Get(_name);

    /// <summary>
    /// Gets the options instance with the specified name, or the configured name if null.
    /// </summary>
    /// <param name="name">The name of the options instance, or null to use the configured name.</param>
    /// <returns>The options instance.</returns>
    public TOptions Get(string? name) => _monitor.Get(name ?? _name);

    /// <summary>
    /// Registers a change callback that will be invoked when the options change.
    /// This enables reactive scenarios like API key rotation.
    /// Only invokes the listener for changes to this specific named instance.
    /// </summary>
    /// <param name="listener">The callback to invoke when options change.</param>
    /// <returns>A disposable that removes the change callback when disposed.</returns>
    public IDisposable OnChange(Action<TOptions, string> listener) =>
        _monitor.OnChange((options, name) =>
        {
            if (name == _name)
            {
                listener(options, name);
            }
        });
}