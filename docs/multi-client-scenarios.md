# Multi-Client Scenarios Guide

This guide covers advanced scenarios where you need to work with multiple Kontent.ai environments, brands, or configurations within a single application.

## Table of Contents

- [Overview](#overview)
- [Use Cases](#use-cases)
- [Named Clients](#named-clients)
  - [Registration](#registration)
  - [Accessing Named Clients](#accessing-named-clients)
  - [Keyed Services (.NET 8+)](#keyed-services-net-8)
- [Client Factory](#client-factory)
- [Multi-Tenant Architectures](#multi-tenant-architectures)
  - [Fixed Tenants](#fixed-tenants)
  - [Dynamic Tenant Resolution](#dynamic-tenant-resolution)
  - [Tenant-Specific Configuration](#tenant-specific-configuration)
- [Multi-Brand Scenarios](#multi-brand-scenarios)
- [Preview vs Production](#preview-vs-production)
- [Environment-Specific Configuration](#environment-specific-configuration)
- [Best Practices](#best-practices)
- [Real-World Examples](#real-world-examples)
- [Troubleshooting](#troubleshooting)

## Overview

The SDK supports multiple simultaneous client instances, each with its own configuration. This enables:

- **Multi-tenant applications**: Serve content from different Kontent.ai environments
- **Multi-brand websites**: Manage multiple brands from a single application
- **Preview/production switching**: Dynamically select between preview and production APIs
- **Environment isolation**: Separate development, staging, and production configurations

## Use Cases

### Multi-Tenant SaaS

Each customer has their own Kontent.ai environment:

```csharp
// Customer A's content
var clientA = factory.Get("customer-a");
var contentA = await clientA.GetItem("homepage").ExecuteAsync();

// Customer B's content
var clientB = factory.Get("customer-b");
var contentB = await clientB.GetItem("homepage").ExecuteAsync();
```

### Multi-Brand Platform

One application serving multiple brands:

```csharp
// Nike content
var nikeClient = factory.Get("nike");

// Adidas content
var adidasClient = factory.Get("adidas");
```

### Content Preview

Switch between production and preview:

```csharp
var client = isPreview
    ? factory.Get("preview")
    : factory.Get("production");
```

### Aggregated Content

Combine content from multiple sources:

```csharp
var globalContent = await globalClient.GetItem("global-settings").ExecuteAsync();
var regionalContent = await regionalClient.GetItem("regional-offers").ExecuteAsync();
```

## Named Clients

### Registration

Register multiple clients with unique names:

```csharp
using Kontent.Ai.Delivery;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register first client
services.AddDeliveryClient("brand-a", options =>
{
    options.EnvironmentId = "brand-a-environment-id";
});

// Register second client
services.AddDeliveryClient("brand-b", options =>
{
    options.EnvironmentId = "brand-b-environment-id";
});

// Register third client with caching (requires Kontent.Ai.Delivery.Caching package)
services.AddDeliveryClient("brand-c", options =>
{
    options.EnvironmentId = "brand-c-environment-id";
});
services.AddDeliveryMemoryCache("brand-c", defaultExpiration: TimeSpan.FromHours(1));

var serviceProvider = services.BuildServiceProvider();
```

### Accessing Named Clients

#### Using IDeliveryClientFactory

```csharp
public class ContentService
{
    private readonly IDeliveryClientFactory _factory;

    public ContentService(IDeliveryClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<IContentItem> GetBrandAHomepageAsync()
    {
        var client = _factory.Get("brand-a");
        var result = await client.GetItem("homepage").ExecuteAsync();
        return result.Value;
    }

    public async Task<IContentItem> GetBrandBHomepageAsync()
    {
        var client = _factory.Get("brand-b");
        var result = await client.GetItem("homepage").ExecuteAsync();
        return result.Value;
    }
}
```

### Keyed Services (.NET 8+)

Use .NET 8's keyed services for cleaner dependency injection:

```csharp
public class BrandAController : ControllerBase
{
    private readonly IDeliveryClient _client;

    // Inject specific client by key
    public BrandAController(
        [FromKeyedServices("brand-a")] IDeliveryClient client)
    {
        _client = client;
    }

    [HttpGet("homepage")]
    public async Task<IActionResult> GetHomepage()
    {
        var result = await _client.GetItem("homepage").ExecuteAsync();

        if (result.IsSuccess)
            return Ok(result.Value);

        return NotFound();
    }
}
```

## Client Factory

### Basic Factory Usage

```csharp
public interface IDeliveryClientFactory
{
    IDeliveryClient Get(string name);
}

// Usage
var factory = serviceProvider.GetRequiredService<IDeliveryClientFactory>();
var client = factory.Get("brand-a");
```

### Factory with Fallback

```csharp
public class SafeClientFactory
{
    private readonly IDeliveryClientFactory _factory;
    private readonly ILogger<SafeClientFactory> _logger;

    public IDeliveryClient GetClient(string name, string fallbackName = "default")
    {
        try
        {
            return _factory.Get(name);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Client {Name} not found, using fallback {Fallback}",
                name, fallbackName);
            return _factory.Get(fallbackName);
        }
    }
}
```

### Typed Factory

Create a strongly-typed factory for specific clients:

```csharp
public interface IBrandClientFactory
{
    IDeliveryClient Nike { get; }
    IDeliveryClient Adidas { get; }
    IDeliveryClient Puma { get; }
}

public class BrandClientFactory : IBrandClientFactory
{
    private readonly IDeliveryClientFactory _factory;

    public BrandClientFactory(IDeliveryClientFactory factory)
    {
        _factory = factory;
    }

    public IDeliveryClient Nike => _factory.Get("nike");
    public IDeliveryClient Adidas => _factory.Get("adidas");
    public IDeliveryClient Puma => _factory.Get("puma");
}

// Registration
services.AddSingleton<IBrandClientFactory, BrandClientFactory>();

// Usage
public class ProductController
{
    private readonly IBrandClientFactory _brands;

    public ProductController(IBrandClientFactory brands)
    {
        _brands = brands;
    }

    public async Task<IActionResult> GetNikeProducts()
    {
        var result = await _brands.Nike
            .GetItems<Product>()
            .ExecuteAsync();

        return Ok(result.Value);
    }
}
```

## Multi-Tenant Architectures

### Fixed Tenants

When you have a known set of tenants:

```csharp
// appsettings.json
{
  "Tenants": [
    {
      "Name": "tenant1",
      "EnvironmentId": "guid-1",
      "CacheExpiration": "01:00:00"
    },
    {
      "Name": "tenant2",
      "EnvironmentId": "guid-2",
      "CacheExpiration": "02:00:00"
    }
  ]
}

// Startup configuration
var tenants = configuration.GetSection("Tenants").Get<List<TenantConfig>>();

foreach (var tenant in tenants)
{
    services.AddDeliveryClient(tenant.Name, options =>
    {
        options.EnvironmentId = tenant.EnvironmentId;
    });
    services.AddDeliveryMemoryCache(tenant.Name, defaultExpiration: tenant.CacheExpiration);
}
```

### Dynamic Tenant Resolution

Resolve tenant from request context:

```csharp
public class TenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDeliveryClientFactory _factory;

    public TenantResolver(
        IHttpContextAccessor httpContextAccessor,
        IDeliveryClientFactory factory)
    {
        _httpContextAccessor = httpContextAccessor;
        _factory = factory;
    }

    public IDeliveryClient GetCurrentTenantClient()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // Option 1: From subdomain
        var host = httpContext.Request.Host.Host;
        var subdomain = host.Split('.')[0];
        return _factory.Get(subdomain);

        // Option 2: From route
        var tenantId = httpContext.Request.RouteValues["tenantId"]?.ToString();
        return _factory.Get(tenantId);

        // Option 3: From header
        var tenantHeader = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        return _factory.Get(tenantHeader);

        // Option 4: From claims
        var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
        return _factory.Get(tenantClaim);
    }
}

// Registration
services.AddHttpContextAccessor();
services.AddScoped<TenantResolver>();

// Usage in controller
public class ContentController : ControllerBase
{
    private readonly TenantResolver _tenantResolver;

    public ContentController(TenantResolver tenantResolver)
    {
        _tenantResolver = tenantResolver;
    }

    [HttpGet("content/{codename}")]
    public async Task<IActionResult> GetContent(string codename)
    {
        var client = _tenantResolver.GetCurrentTenantClient();
        var result = await client.GetItem(codename).ExecuteAsync();

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}
```

### Tenant-Specific Configuration

Different configurations per tenant:

```csharp
public class TenantConfig
{
    public string Name { get; set; }
    public string EnvironmentId { get; set; }
    public bool UsePreview { get; set; }
    public string? PreviewApiKey { get; set; }
    public TimeSpan CacheExpiration { get; set; }
    public bool EnableResilience { get; set; }
}

public static class TenantRegistration
{
    public static void RegisterTenants(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var tenants = configuration
            .GetSection("Tenants")
            .Get<List<TenantConfig>>();

        foreach (var tenant in tenants)
        {
            services.AddDeliveryClient(tenant.Name, options =>
            {
                options.EnvironmentId = tenant.EnvironmentId;
                options.UsePreviewApi = tenant.UsePreview;
                options.PreviewApiKey = tenant.PreviewApiKey;
                options.EnableResilience = tenant.EnableResilience;
            });

            if (tenant.CacheExpiration > TimeSpan.Zero)
            {
                services.AddDeliveryMemoryCache(tenant.Name, defaultExpiration: tenant.CacheExpiration);
            }
        }
    }
}
```

## Multi-Brand Scenarios

### Brand-Specific Services

```csharp
public interface IBrandContentService
{
    Task<HomePage> GetHomePageAsync();
    Task<IEnumerable<Product>> GetFeaturedProductsAsync();
}

public class NikeBrandService : IBrandContentService
{
    private readonly IDeliveryClient _client;

    public NikeBrandService([FromKeyedServices("nike")] IDeliveryClient client)
    {
        _client = client;
    }

    public async Task<HomePage> GetHomePageAsync()
    {
        var result = await _client.GetItem<HomePage>("homepage").ExecuteAsync();
        return result.Value;
    }

    public async Task<IEnumerable<Product>> GetFeaturedProductsAsync()
    {
        var result = await _client.GetItems<Product>()
            .Where(f => f.Element("tags").ContainsAny("featured"))
            .Limit(10)
            .ExecuteAsync();
        return result.Value;
    }
}

// Register brand services
services.AddScoped<NikeBrandService>();
services.AddScoped<AdidasBrandService>();
```

### Brand Router

Route to appropriate brand based on URL:

```csharp
public class BrandRouter
{
    private readonly IDeliveryClientFactory _factory;
    private readonly Dictionary<string, string> _hostToBrand;

    public BrandRouter(IDeliveryClientFactory factory, IConfiguration config)
    {
        _factory = factory;
        _hostToBrand = config
            .GetSection("BrandHosts")
            .Get<Dictionary<string, string>>();
    }

    public IDeliveryClient GetClientForHost(string host)
    {
        if (_hostToBrand.TryGetValue(host, out var brandName))
        {
            return _factory.Get(brandName);
        }

        throw new InvalidOperationException($"No brand configured for host: {host}");
    }
}

// appsettings.json
{
  "BrandHosts": {
    "nike.com": "nike",
    "adidas.com": "adidas",
    "puma.com": "puma"
  }
}
```

### Aggregated Brand Content

Combine content from multiple brands:

```csharp
public class AggregatedContentService
{
    private readonly IDeliveryClientFactory _factory;
    private readonly string[] _brandNames = { "nike", "adidas", "puma" };

    public async Task<IEnumerable<Product>> GetAllFeaturedProductsAsync()
    {
        var tasks = _brandNames.Select(async brandName =>
        {
            var client = _factory.Get(brandName);
            var result = await client.GetItems<Product>()
                .Where(f => f.Element("tags").ContainsAny("featured"))
                .Limit(5)
                .ExecuteAsync();

            return result.IsSuccess ? result.Value : Enumerable.Empty<Product>();
        });

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(x => x);
    }
}
```

## Preview vs Production

### Separate Clients for Preview and Production

```csharp
services.AddDeliveryClient("production", options =>
{
    options.EnvironmentId = "your-environment-id";
    options.UsePreviewApi = false;
});
services.AddDeliveryMemoryCache("production", defaultExpiration: TimeSpan.FromHours(2));

services.AddDeliveryClient("preview", options =>
{
    options.EnvironmentId = "your-environment-id";
    options.UsePreviewApi = true;
    options.PreviewApiKey = "your-preview-api-key";
});
services.AddDeliveryMemoryCache("preview", defaultExpiration: TimeSpan.FromMinutes(5));
```

`UsePreviewApi = true` clients always bypass SDK cache reads/writes. Registering a cache manager for preview is optional and does not change that behavior.

### Preview Mode Service

```csharp
public class ContentPreviewService
{
    private readonly IDeliveryClientFactory _factory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IDeliveryClient GetClient()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // Check for preview mode (from query string, cookie, or user claim)
        var isPreview = httpContext.Request.Query.ContainsKey("preview") ||
                       httpContext.Request.Cookies.ContainsKey("preview_mode") ||
                       httpContext.User.IsInRole("ContentEditor");

        return _factory.Get(isPreview ? "preview" : "production");
    }
}

// Usage in controller
public class ArticleController : ControllerBase
{
    private readonly ContentPreviewService _previewService;

    [HttpGet("{codename}")]
    public async Task<IActionResult> GetArticle(string codename)
    {
        var client = _previewService.GetClient();
        var result = await client.GetItem<Article>(codename).ExecuteAsync();

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}
```

### Preview Toggle Middleware

```csharp
public class PreviewModeMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Enable preview mode with ?preview=true&key=secret
        if (context.Request.Query.TryGetValue("preview", out var preview) &&
            preview == "true" &&
            ValidatePreviewKey(context.Request.Query["key"]))
        {
            context.Response.Cookies.Append("preview_mode", "true", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });
        }

        // Disable preview mode
        if (context.Request.Query["preview"] == "false")
        {
            context.Response.Cookies.Delete("preview_mode");
        }

        await _next(context);
    }

    private bool ValidatePreviewKey(string key)
    {
        // Validate preview access key
        return key == "your-secret-preview-key";
    }
}

// Register middleware
app.UseMiddleware<PreviewModeMiddleware>();
```

## Environment-Specific Configuration

### Environment-Based Registration

```csharp
public static class DeliveryClientRegistration
{
    public static void AddKontentDeliveryClients(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            // Development: shorter cache, preview API
            services.AddDeliveryClient("default", options =>
            {
                options.EnvironmentId = configuration["Kontent:EnvironmentId"];
                options.UsePreviewApi = true;
                options.PreviewApiKey = configuration["Kontent:PreviewApiKey"];
            });
            services.AddDeliveryMemoryCache("default", defaultExpiration: TimeSpan.FromMinutes(5));
        }
        else if (environment.IsStaging())
        {
            // Staging: moderate cache, production API
            services.AddDeliveryClient("default", options =>
            {
                options.EnvironmentId = configuration["Kontent:EnvironmentId"];
                options.UsePreviewApi = false;
            });
            services.AddDeliveryMemoryCache("default", defaultExpiration: TimeSpan.FromMinutes(30));
        }
        else // Production
        {
            // Production: hybrid cache, production API, resilience
            services.AddDeliveryClient("default", options =>
            {
                options.EnvironmentId = configuration["Kontent:EnvironmentId"];
                options.EnableResilience = true;
            });
            services.AddDeliveryHybridCache("default", defaultExpiration: TimeSpan.FromHours(4));
        }
    }
}
```

## Best Practices

### 1. Use Descriptive Client Names

```csharp
// Good: Clear, descriptive names
services.AddDeliveryClient("corporate-website", ...);
services.AddDeliveryClient("e-commerce-platform", ...);
services.AddDeliveryClient("customer-portal-preview", ...);

// Bad: Cryptic names
services.AddDeliveryClient("c1", ...);
services.AddDeliveryClient("env2", ...);
```

### 2. Centralize Client Configuration

```csharp
public static class DeliveryClientsConfiguration
{
    public static void ConfigureClients(IServiceCollection services, IConfiguration config)
    {
        var clients = config.GetSection("DeliveryClients").Get<DeliveryClientConfig[]>();

        foreach (var client in clients)
        {
            services.AddDeliveryClient(client.Name, options =>
            {
                options.EnvironmentId = client.EnvironmentId;
                // ... other configuration
            });
        }
    }
}
```

### 3. Document Client Purpose

```csharp
/// <summary>
/// Delivery clients configuration:
/// - "production": Main production content
/// - "preview": Preview API for editors
/// - "fallback": Fallback environment for testing
/// </summary>
public static void RegisterDeliveryClients(IServiceCollection services)
{
    // ...
}
```

### 4. Handle Missing Clients Gracefully

```csharp
public IDeliveryClient GetClientSafely(string name)
{
    try
    {
        return _factory.Get(name);
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogError(ex, "Client {Name} not found", name);
        return _factory.Get("default");  // Fallback to default
    }
}
```

### 5. Monitor Per-Client Metrics

```csharp
public class MonitoredClientFactory : IDeliveryClientFactory
{
    private readonly IDeliveryClientFactory _inner;
    private readonly IMetrics _metrics;

    public IDeliveryClient Get(string name)
    {
        _metrics.IncrementClientAccess(name);
        return _inner.Get(name);
    }
}
```

## Real-World Examples

### Multi-Region E-Commerce

```csharp
public class RegionalContentService
{
    private readonly IDeliveryClientFactory _factory;
    private readonly Dictionary<string, string> _regionToClient = new()
    {
        ["us"] = "us-commerce",
        ["eu"] = "eu-commerce",
        ["apac"] = "apac-commerce"
    };

    public async Task<IEnumerable<Product>> GetRegionalProductsAsync(string region)
    {
        var clientName = _regionToClient.GetValueOrDefault(region, "us-commerce");
        var client = _factory.Get(clientName);

        var result = await client.GetItems<Product>()
            .Where(f => f.System("type").IsEqualTo("product"))
            .Where(f => f.Element("stock").IsGreaterThan(0.0))
            .ExecuteAsync();

        return result.Value;
    }
}
```

### White-Label SaaS Platform

```csharp
public class WhiteLabelContentService
{
    private readonly IDeliveryClientFactory _factory;

    public async Task<SiteConfiguration> GetClientConfigurationAsync(Guid clientId)
    {
        var clientName = $"client-{clientId}";
        var client = _factory.Get(clientName);

        var result = await client.GetItem<SiteConfiguration>("site_config")
            .ExecuteAsync();

        return result.Value;
    }

    public async Task<IEnumerable<Page>> GetClientPagesAsync(Guid clientId)
    {
        var clientName = $"client-{clientId}";
        var client = _factory.Get(clientName);

        var result = await client.GetItems<Page>()
            .ExecuteAsync();

        return result.Value;
    }
}
```

## Troubleshooting

### Client Not Found

**Problem**: `InvalidOperationException: No client registered with name 'xyz'`

**Solution**: Verify client registration:

```csharp
// List all registered clients (for debugging)
var factory = serviceProvider.GetRequiredService<IDeliveryClientFactory>();

// Try to get client in try-catch
try
{
    var client = factory.Get("xyz");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Client 'xyz' not found. Registered clients: ...");
}
```

### Wrong Environment ID

**Problem**: Content from wrong environment is returned.

**Solution**: Log and verify environment IDs:

```csharp
services.AddDeliveryClient("brand-a", options =>
{
    var envId = configuration["BrandA:EnvironmentId"];
    Console.WriteLine($"Registering brand-a with environment: {envId}");
    options.EnvironmentId = envId;
});
```

### Cache Collisions

**Problem**: Cached content from one client appears for another.

**Solution**: Ensure each client uses its own cache namespace. The SDK does this with per-client key prefixes for named clients (or custom `keyPrefix` values when configured). If you change `EnvironmentId` on an already-cached client at runtime, purge cache or recreate the client.

---

**Related Documentation**:
- [Main README](../README.md)
- [Caching Guide](caching-guide.md)
- [Performance Optimization Guide](performance-optimization.md)
