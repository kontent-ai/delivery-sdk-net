using System.Reflection;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Default type provider that attempts to discover a source-generated type provider,
/// falling back to returning null for all mappings (enabling dynamic types).
/// </summary>
/// <remarks>
/// <para>
/// This class performs deterministic discovery of generated type providers without
/// scanning the entire AppDomain. Discovery order:
/// </para>
/// <list type="number">
///   <item>Check entry assembly for <c>Kontent.Ai.Delivery.Generated.GeneratedTypeProvider</c></item>
///   <item>Check assemblies referenced by entry assembly (bounded set)</item>
///   <item>Check calling assembly and its references (for test scenarios)</item>
/// </list>
/// <para>
/// If no generated type provider is found, returns null for all mappings, instructing
/// <see cref="DefaultItemTypingStrategy"/> to use dynamic types.
/// </para>
/// <para>
/// Users can override this behavior by registering their own <see cref="ITypeProvider"/>
/// in the DI container.
/// </para>
/// </remarks>
internal sealed class TypeProvider : ITypeProvider
{
    private const string GeneratedTypeProviderName = "Kontent.Ai.Delivery.Generated.GeneratedTypeProvider";

    private static readonly Lazy<ITypeProvider?> _discoveredProvider = new(DiscoverGeneratedProvider);

    public Type? GetType(string contentType)
        => _discoveredProvider.Value?.GetType(contentType);

    public string? GetCodename(Type contentType)
        => _discoveredProvider.Value?.GetCodename(contentType);

    private static ITypeProvider? DiscoverGeneratedProvider()
    {
        // 1. Check entry assembly first
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            var provider = TryCreateProviderFromAssembly(entryAssembly);
            if (provider != null)
                return provider;

            // 2. Check referenced assemblies (bounded set)
            var referencedAssemblies = GetReferencedAssemblies(entryAssembly);
            foreach (var assembly in referencedAssemblies)
            {
                provider = TryCreateProviderFromAssembly(assembly);
                if (provider != null)
                    return provider;
            }
        }

        // 3. Fallback: check calling assembly (for test scenarios where entry assembly may be test runner)
        var callingAssembly = Assembly.GetCallingAssembly();
        if (callingAssembly != null && callingAssembly != entryAssembly)
        {
            var provider = TryCreateProviderFromAssembly(callingAssembly);
            if (provider != null)
                return provider;

            var referencedAssemblies = GetReferencedAssemblies(callingAssembly);
            foreach (var assembly in referencedAssemblies)
            {
                provider = TryCreateProviderFromAssembly(assembly);
                if (provider != null)
                    return provider;
            }
        }

        return null;
    }

    private static ITypeProvider? TryCreateProviderFromAssembly(Assembly assembly)
    {
        try
        {
            var providerType = assembly.GetType(GeneratedTypeProviderName);
            if (providerType != null && typeof(ITypeProvider).IsAssignableFrom(providerType))
            {
                return (ITypeProvider?)Activator.CreateInstance(providerType);
            }
        }
        catch
        {
            // Ignore exceptions - assembly might not be loadable
        }
        return null;
    }

    private static IEnumerable<Assembly> GetReferencedAssemblies(Assembly assembly)
    {
        var loaded = new HashSet<string>();

        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            if (loaded.Add(reference.FullName))
            {
                Assembly? referencedAssembly = null;
                try
                {
                    referencedAssembly = Assembly.Load(reference);
                }
                catch
                {
                    // Ignore - assembly might not be loadable
                }

                if (referencedAssembly != null)
                    yield return referencedAssembly;
            }
        }
    }
}
