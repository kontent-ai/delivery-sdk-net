using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Kontent.Ai.Core.Configuration;
using Kontent.Ai.Core.Extensions;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.Handlers;
using Kontent.Ai.Urls.Delivery.QueryParameters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Refit;

namespace Kontent.Ai.Delivery.Extensions;

/// <summary>
/// Service collection extensions for registering Delivery SDK using Core package infrastructure.
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Delivery SDK services using the Core package infrastructure with Refit-based API clients.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <param name="configureOptions">Action to configure the delivery client options.</param>
    /// <param name="configureResilience">Optional action to configure resilience strategies.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryClient(
        this IServiceCollection services,
        Action<DeliveryOptions> configureOptions,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>, DeliveryOptions>? configureResilience = null)
    {
        // Register delivery-specific SDK identity for tracking
        var deliveryIdentity = new SdkIdentity("Kontent.Ai.Delivery", GetDeliveryVersion());
        
        // Register Core services with Delivery SDK identity
        services.AddCore(coreOptions =>
        {
            coreOptions = coreOptions with { SdkIdentity = deliveryIdentity };
        });

        // Register the Refit API clients using Core's AddClient method
        services.AddClient<IDeliveryContentApi, DeliveryOptions>(
            options =>
            {
                configureOptions(options);
            },
            configureRefitSettings: settings =>
            {
                // Use System.Text.Json for compatibility with Core package
                settings.ContentSerializer = new SystemTextJsonContentSerializer();
            },
            configureHttpClient: httpClient =>
            {
                // Base address is set by Core package from options.BaseUrl
            },
            configureResilience: configureResilience);

        services.AddClient<IDeliveryMetadataApi, DeliveryOptions>(
            options =>
            {
                configureOptions(options);
            },
            configureRefitSettings: settings =>
            {
                settings.ContentSerializer = new SystemTextJsonContentSerializer();
            },
            configureHttpClient: httpClient =>
            {
                // Base address is set by Core package from options.BaseUrl
            },
            configureResilience: configureResilience);

        services.AddClient<IDeliverySyncApi, DeliveryOptions>(
            options =>
            {
                configureOptions(options);
            },
            configureRefitSettings: settings =>
            {
                settings.ContentSerializer = new SystemTextJsonContentSerializer();
            },
            configureHttpClient: httpClient =>
            {
                // Base address is set by Core package from options.BaseUrl
            },
            configureResilience: configureResilience);

        services.AddClient<IDeliverySyncV2Api, DeliveryOptions>(
            options =>
            {
                configureOptions(options);
                // Set base URL for the Core package - sync v2 endpoints (v2 prefix + environment ID)
            },
            configureRefitSettings: settings =>
            {
                settings.ContentSerializer = new SystemTextJsonContentSerializer();
            },
            configureHttpClient: httpClient =>
            {
                // Base address is set by Core package from options.BaseUrl
            },
            configureResilience: configureResilience);

        // TODO: Register the new delivery client implementation
        // services.AddTransient<IDeliveryClient, DeliveryClientRefit>();

        return services;
    }

    /// <summary>
    /// Registers Delivery SDK services using configuration section.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configurationSectionName">The configuration section name (default: "DeliveryOptions").</param>
    /// <param name="configureResilience">Optional action to configure resilience strategies.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDelivery(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = "DeliveryOptions",
        Action<ResiliencePipelineBuilder<HttpResponseMessage>, DeliveryOptions>? configureResilience = null)
    {
        var configurationSection = configuration.GetSection(configurationSectionName);
        return services.AddDeliveryClient(configurationSection.Bind, configureResilience);
    }

    /// <summary>
    /// Converts query parameters to dictionary format for Refit.
    /// </summary>
    /// <param name="parameters">The query parameters.</param>
    /// <returns>Dictionary of query parameters.</returns>
    internal static IDictionary<string, object> ToQueryDictionary(this IEnumerable<IQueryParameter> parameters)
    {
        if (parameters == null) return new Dictionary<string, object>();

        var queryDict = new Dictionary<string, object>();
        foreach (var parameter in parameters)
        {
            var queryString = parameter.GetQueryStringParameter();
            var parts = queryString.Split('=', 2);
            if (parts.Length == 2)
            {
                queryDict[parts[0]] = parts[1];
            }
        }
        return queryDict;
    }

    private static Version GetDeliveryVersion()
    {
        var assembly = typeof(ServiceCollectionExtensions).Assembly;
        var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        if (versionAttribute?.InformationalVersion != null)
        {
            var versionPart = versionAttribute.InformationalVersion.Split('-')[0];
            if (Version.TryParse(versionPart, out var version))
            {
                return version;
            }
        }

        return assembly.GetName().Version ?? new Version(1, 0, 0);
    }
} 