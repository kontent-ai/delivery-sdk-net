using System.Threading;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Resolution;

/// <inheritdoc cref="IHtmlResolutionContext" />
internal sealed class HtmlResolutionContext : IHtmlResolutionContext
{
    public IReadOnlyDictionary<string, object>? LinkedItems { get; init; }

    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

    public IServiceProvider? Services { get; init; }
}
