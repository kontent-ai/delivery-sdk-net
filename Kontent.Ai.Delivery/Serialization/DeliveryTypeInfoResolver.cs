using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Kontent.Ai.Delivery.Serialization;

/// <summary>
/// Custom JsonTypeInfoResolver that integrates with dependency injection for creating instances.
/// </summary>
internal class DeliveryTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;

    public DeliveryTypeInfoResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1000
        });
    }

    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var cacheKey = $"JsonTypeInfo_{type.FullName}";

        if (_cache.TryGetValue(cacheKey, out JsonTypeInfo cachedTypeInfo))
        {
            return cachedTypeInfo;
        }

        var typeInfo = base.GetTypeInfo(type, options);

        // If it's an interface, try to resolve implementation from DI
        if (type.IsInterface)
        {
            var services = _serviceProvider.GetServices(type);
            if (services != null && services.Any())
            {
                var implementation = GetClosestImplementation(services, type);
                if (implementation != null)
                {
                    // Get type info for the implementation
                    var implementationTypeInfo = base.GetTypeInfo(implementation, options);

                    // Set up custom creator that uses DI
                    if (implementationTypeInfo.CreateObject == null)
                    {
                        implementationTypeInfo.CreateObject = () =>
                            _serviceProvider.GetService(implementation) ?? Activator.CreateInstance(implementation);
                    }

                    _cache.Set(cacheKey, implementationTypeInfo, TimeSpan.FromMinutes(30));
                    return implementationTypeInfo;
                }
            }
        }

        // For non-interface types, try to get from DI first, then fall back to default creation
        if (typeInfo.CreateObject == null)
        {
            typeInfo.CreateObject = () =>
            {
                var instance = _serviceProvider.GetService(type);
                if (instance != null)
                {
                    return instance;
                }

                // Fall back to default creation
                try
                {
                    return Activator.CreateInstance(type);
                }
                catch (MissingMethodException)
                {
                    // Type has no parameterless constructor
                    return null;
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    // Log error if logger is available
                    // For now, return null and let the serializer handle it
                    return null;
                }
            };
        }

        _cache.Set(cacheKey, typeInfo, TimeSpan.FromMinutes(30));
        return typeInfo;
    }

    private Type GetClosestImplementation(System.Collections.Generic.IEnumerable<object> services, Type interfaceType)
    {
        return services
            .Select(s => s.GetType())
            .FirstOrDefault(type => type.GetInterfaces().Contains(interfaceType));
    }
}