using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Kontent.Ai.Delivery.Extensions;

internal static class HttpRequestHeadersExtensions
{
    private const string SdkTrackingHeaderName = "X-KC-SDKID";
    private const string SourceTrackingHeaderName = "X-KC-SOURCE";
    private const string PackageRepositoryHost = "nuget.org";

    private static readonly Lazy<string> Sdk = new(GetSdk);
    private static readonly Lazy<string?> Source = new(GetSource);
    public const string WaitForLoadingNewContentHeaderName = "X-KC-Wait-For-Loading-New-Content";

    internal static void AddSdkTrackingHeader(this HttpRequestHeaders headers) => headers.Add(SdkTrackingHeaderName, Sdk.Value);

    /// <summary>
    /// Adds a tracking header according to https://kontent-ai.github.io/articles/Guidelines-for-Kontent.ai-related-tools.html#analytics
    /// </summary>
    /// <param name="headers">Collection of headers</param>
    internal static void AddSourceTrackingHeader(this HttpRequestHeaders headers)
    {
        var source = Source.Value;
        if (source is not null)
        {
            headers.Add(SourceTrackingHeaderName, source);
        }
    }

    internal static string GetProductVersion(this Assembly assembly)
    {
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        return StripBuildMetadata(informationalVersion) ?? "0.0.0";
    }

    /// <summary>
    /// Removes the SemVer build-metadata suffix (everything from '+' onward) from a version string.
    /// Preserves pre-release suffixes (for example <c>-rc.5</c>). Returns <c>null</c> if the input is null/empty/whitespace.
    /// </summary>
    internal static string? StripBuildMetadata(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        var plusIndex = version.IndexOf('+', StringComparison.Ordinal);
        return plusIndex < 0 ? version : version[..plusIndex];
    }

    internal static string GetSdk()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var sdkVersion = assembly.GetProductVersion();
        var sdkPackageId = assembly.GetName().Name;

        return $"{PackageRepositoryHost};{sdkPackageId};{sdkVersion}";
    }

    /// <summary>
    /// Gets the SDK version string for logging purposes.
    /// </summary>
    internal static string GetSdkVersion() => Sdk.Value;

    internal static string? GetSource()
    {
        var originatingAssembly = GetOriginatingAssembly();
        if (originatingAssembly is not null)
        {
            var attribute = originatingAssembly.GetCustomAttributes<DeliverySourceTrackingHeaderAttribute>().FirstOrDefault();
            if (attribute is not null)
            {
                return GenerateSourceTrackingHeaderValue(originatingAssembly, attribute);
            }
        }
        return null;
    }

    internal static string GenerateSourceTrackingHeaderValue(Assembly originatingAssembly, DeliverySourceTrackingHeaderAttribute attribute)
    {
        string? packageName;
        string version;
        if (attribute.LoadFromAssembly)
        {
            packageName = attribute.PackageName ?? originatingAssembly.GetName().Name;
            version = originatingAssembly.GetProductVersion();
        }
        else
        {
            packageName = attribute.PackageName;
            var preRelease = attribute.PreReleaseLabel is null ? "" : $"-{attribute.PreReleaseLabel}";
            version = $"{attribute.MajorVersion}.{attribute.MinorVersion}.{attribute.PatchVersion}{preRelease}";
        }
        return $"{packageName};{version}";
    }

    /// <summary>
    /// Gets the first assembly in the call chain.
    /// </summary>
    /// <returns>The first assembly in the call stack.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Assembly? GetOriginatingAssembly()
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        // Get the whole stack trace, get involved assemblies, and determine which one references this SDK
        var callerAssemblies = new StackTrace().GetFrames()
                    .Select(x => x.GetMethod()?.ReflectedType?.Assembly).Distinct().OfType<Assembly>()
                    .Where(x => x.GetReferencedAssemblies().Any(y => y.FullName == executingAssembly.FullName));
        var originatingAssembly = callerAssemblies.LastOrDefault();
        return originatingAssembly;
    }
}
