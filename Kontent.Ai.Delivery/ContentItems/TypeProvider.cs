using System.Reflection;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Default type provider that attempts to discover a generated ContentTypeRegistry,
/// falling back to returning null for all mappings (enabling dynamic types).
/// </summary>
/// <remarks>
/// <para>
/// This class performs deterministic discovery of generated type registries without
/// scanning the entire AppDomain. Discovery order:
/// </para>
/// <list type="number">
///   <item>Check entry assembly for <c>Kontent.Ai.Delivery.Generated.ContentTypeRegistry</c></item>
///   <item>Check assemblies referenced by entry assembly (bounded set)</item>
/// </list>
/// <para>
/// If no generated registry is found, returns null for all mappings, instructing
/// <see cref="DefaultItemTypingStrategy"/> to use dynamic types.
/// </para>
/// <para>
/// Users can override this behavior by registering their own <see cref="ITypeProvider"/>
/// in the DI container.
/// </para>
/// </remarks>
internal class TypeProvider : ITypeProvider
{
    private const string GeneratedRegistryTypeName = "Kontent.Ai.Delivery.Generated.ContentTypeRegistry";

    private static readonly Lazy<ITypeProvider?> _discoveredRegistry = new(DiscoverRegistry);

    public Type? GetType(string contentType)
        => _discoveredRegistry.Value?.GetType(contentType);

    public string? GetCodename(Type contentType)
        => _discoveredRegistry.Value?.GetCodename(contentType);

    private static ITypeProvider? DiscoverRegistry()
    {
        // 1. Check entry assembly first
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            var registry = TryCreateRegistryFromAssembly(entryAssembly);
            if (registry != null)
                return registry;

            // 2. Check referenced assemblies (bounded set)
            var referencedAssemblies = GetReferencedAssemblies(entryAssembly);
            foreach (var assembly in referencedAssemblies)
            {
                registry = TryCreateRegistryFromAssembly(assembly);
                if (registry != null)
                    return registry;
            }
        }

        // 3. Fallback: check calling assembly (for test scenarios where entry assembly may be test runner)
        var callingAssembly = Assembly.GetCallingAssembly();
        if (callingAssembly != null && callingAssembly != entryAssembly)
        {
            var registry = TryCreateRegistryFromAssembly(callingAssembly);
            if (registry != null)
                return registry;

            var referencedAssemblies = GetReferencedAssemblies(callingAssembly);
            foreach (var assembly in referencedAssemblies)
            {
                registry = TryCreateRegistryFromAssembly(assembly);
                if (registry != null)
                    return registry;
            }
        }

        return null;
    }

    private static ITypeProvider? TryCreateRegistryFromAssembly(Assembly assembly)
    {
        try
        {
            var registryType = assembly.GetType(GeneratedRegistryTypeName);
            if (registryType != null && typeof(ITypeProvider).IsAssignableFrom(registryType))
            {
                return (ITypeProvider?)Activator.CreateInstance(registryType);
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
