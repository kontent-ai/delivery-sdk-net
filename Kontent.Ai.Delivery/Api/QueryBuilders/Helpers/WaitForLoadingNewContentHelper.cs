namespace Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;

internal static class WaitForLoadingNewContentHelper
{
    internal const string CacheModeDefault = "default";
    internal const string CacheModeEnabled = "enabled";
    internal const string CacheModeDisabled = "disabled";
    internal const string CacheModeEnabledByDefault = "enabled-default";

    internal static bool? ResolveHeaderValue(bool? waitOverride, bool? defaultWaitForNewContent)
    {
        return waitOverride switch
        {
            true => true,
            // Explicit disable means "omit header", regardless of global default.
            false => null,
            _ => defaultWaitForNewContent
        };
    }

    internal static bool ShouldBypassCache(bool? waitOverride, bool? defaultWaitForNewContent)
        => ResolveHeaderValue(waitOverride, defaultWaitForNewContent) == true;

    internal static string ResolveCacheMode(bool? waitOverride, bool? defaultWaitForNewContent)
    {
        return waitOverride switch
        {
            true => CacheModeEnabled,
            false => CacheModeDisabled,
            _ => defaultWaitForNewContent == true ? CacheModeEnabledByDefault : CacheModeDefault
        };
    }
}
