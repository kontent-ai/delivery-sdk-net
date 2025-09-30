using System.Text.Json;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Kontent.Ai.Delivery.ContentItems.InlineContentItems;
using Kontent.Ai.Delivery.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Kontent.Ai.Delivery.Extensions;

/// <summary>
/// Extension methods for registering Kontent.ai Delivery SDK services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Kontent.ai Delivery client with the specified options instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="deliveryOptions">The delivery options instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryClient(
        this IServiceCollection services,
        DeliveryOptions deliveryOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(deliveryOptions);

        services.Configure<DeliveryOptions>(options =>
        {
            options.EnvironmentId = deliveryOptions.EnvironmentId;
            options.PreviewApiKey = deliveryOptions.PreviewApiKey;
            options.SecureAccessApiKey = deliveryOptions.SecureAccessApiKey;
            options.UsePreviewApi = deliveryOptions.UsePreviewApi;
            options.UseSecureAccess = deliveryOptions.UseSecureAccess;
            options.EnableResilience = deliveryOptions.EnableResilience;
            options.ProductionEndpoint = deliveryOptions.ProductionEndpoint;
            options.PreviewEndpoint = deliveryOptions.PreviewEndpoint;
            options.IncludeTotalCount = deliveryOptions.IncludeTotalCount;
            options.WaitForLoadingNewContent = deliveryOptions.WaitForLoadingNewContent;
            options.RenderRichTextToHtml = deliveryOptions.RenderRichTextToHtml;
            options.DefaultRenditionPreset = deliveryOptions.DefaultRenditionPreset;
        });

        return services.AddDeliveryCore();
    }

    /// <summary>
    /// Registers the Kontent.ai Delivery client using configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="configurationSectionName">The configuration section name. Defaults to "DeliveryOptions".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = "DeliveryOptions")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = string.IsNullOrWhiteSpace(configurationSectionName)
            ? configuration
            : configuration.GetSection(configurationSectionName);

        services.Configure<DeliveryOptions>(section);

        return services.AddDeliveryCore();
    }

    /// <summary>
    /// Registers the Kontent.ai Delivery client with configuration action.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the delivery options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryClient(
        this IServiceCollection services,
        Action<DeliveryOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);

        return services.AddDeliveryCore();
    }

    /// <summary>
    /// Registers the Kontent.ai Delivery client with advanced configuration options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the delivery options.</param>
    /// <param name="configureHttpClient">Optional action to configure the HTTP client.</param>
    /// <param name="configureResilience">Optional action to configure resilience policies.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryClient(
        this IServiceCollection services,
        Action<DeliveryOptions> configureOptions,
        Action<IHttpClientBuilder>? configureHttpClient,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);

        return services.AddDeliveryCore(configureHttpClient, configureResilience);
    }

    /// <summary>
    /// Registers the Kontent.ai Delivery client with options instance and advanced configuration.
    /// This overload supports backward compatibility with existing test code.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="deliveryOptions">The delivery options instance.</param>
    /// <param name="configureRefit">Optional action to configure Refit settings.</param>
    /// <param name="configureHttpClient">Optional action to configure the HTTP client.</param>
    /// <param name="configureResilience">Optional action to configure resilience policies.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryClient(
        this IServiceCollection services,
        DeliveryOptions deliveryOptions,
        Action<RefitSettings>? configureRefit = null,
        Action<IHttpClientBuilder>? configureHttpClient = null,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(deliveryOptions);

        services.Configure<DeliveryOptions>(options =>
        {
            options.EnvironmentId = deliveryOptions.EnvironmentId;
            options.PreviewApiKey = deliveryOptions.PreviewApiKey;
            options.SecureAccessApiKey = deliveryOptions.SecureAccessApiKey;
            options.UsePreviewApi = deliveryOptions.UsePreviewApi;
            options.UseSecureAccess = deliveryOptions.UseSecureAccess;
            options.EnableResilience = deliveryOptions.EnableResilience;
            options.ProductionEndpoint = deliveryOptions.ProductionEndpoint;
            options.PreviewEndpoint = deliveryOptions.PreviewEndpoint;
            options.IncludeTotalCount = deliveryOptions.IncludeTotalCount;
            options.WaitForLoadingNewContent = deliveryOptions.WaitForLoadingNewContent;
            options.RenderRichTextToHtml = deliveryOptions.RenderRichTextToHtml;
            options.DefaultRenditionPreset = deliveryOptions.DefaultRenditionPreset;
        });

        return services.AddDeliveryCore(configureHttpClient, configureResilience, configureRefit);
    }

    /// <summary>
    /// Core registration method containing all service registrations.
    /// </summary>
    private static IServiceCollection AddDeliveryCore(
        this IServiceCollection services,
        Action<IHttpClientBuilder>? configureHttpClient = null,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null,
        Action<RefitSettings>? configureRefit = null)
    {
        services.AddOptions<DeliveryOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        RegisterCoreServices(services);
        RegisterHttpClient(services, configureHttpClient, configureResilience, configureRefit);

        services.TryAddSingleton<IDeliveryClient, DeliveryClient>();

        return services;
    }

    /// <summary>
    /// Registers core SDK services.
    /// </summary>
    private static void RegisterCoreServices(IServiceCollection services)
    {
        // JSON serialization
        services.TryAddSingleton(RefitSettingsProvider.CreateDefaultJsonSerializerOptions());

        // HTTP handlers
        services.TryAddTransient<TrackingHandler>();
        services.TryAddTransient<DeliveryAuthenticationHandler>();

        // Core services
        services.TryAddSingleton<IPropertyMapper, PropertyMapper>();
        services.TryAddSingleton<IContentLinkUrlResolver, DefaultContentLinkUrlResolver>();
        services.TryAddSingleton<ITypeProvider, TypeProvider>();
        services.TryAddSingleton<IItemTypingStrategy, DefaultItemTypingStrategy>();
        services.TryAddSingleton<IContentDeserializer, ContentDeserializer>();
        services.TryAddSingleton<IInlineContentItemsProcessor, InlineContentItemsProcessor>();
        services.TryAddSingleton<IElementsPostProcessor, ElementsPostProcessor>();
        services.TryAddSingleton<IHtmlParser, HtmlParser>();

        // Inline content item resolvers
        services.TryAddSingleton<IInlineContentItemsResolver<object>, ReplaceWithWarningAboutRegistrationResolver>();
        services.TryAddSingleton<IInlineContentItemsResolver<UnretrievedContentItem>, ReplaceWithWarningAboutUnretrievedItemResolver>();
        services.TryAddSingleton<IInlineContentItemsResolver<UnknownContentItem>, ReplaceWithWarningAboutUnknownItemResolver>();
    }

    /// <summary>
    /// Registers and configures the HTTP client with Refit.
    /// </summary>
    private static void RegisterHttpClient(
        IServiceCollection services,
        Action<IHttpClientBuilder>? configureHttpClient,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience,
        Action<RefitSettings>? configureRefit)
    {
        var refitSettings = RefitSettingsProvider.CreateDefaultSettings();
        configureRefit?.Invoke(refitSettings);

        var httpClientBuilder = services
            .AddRefitClient<IDeliveryApi>(refitSettings)
            .ConfigureHttpClient((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>().CurrentValue;
                httpClient.BaseAddress = new Uri(options.GetBaseUrl(), UriKind.Absolute);
            });

        // Add resilience handler
        httpClientBuilder.AddResilienceHandler("delivery", (builder, context) =>
        {
            var options = context.ServiceProvider
                .GetRequiredService<IOptionsMonitor<DeliveryOptions>>()
                .CurrentValue;

            if (!options.EnableResilience)
                return;

            if (configureResilience != null)
            {
                configureResilience(builder);
            }
            else
            {
                ConfigureDefaultResilience(builder);
            }
        });

        // Add message handlers
        httpClientBuilder
            .AddHttpMessageHandler<TrackingHandler>()
            .AddHttpMessageHandler<DeliveryAuthenticationHandler>();

        // Apply custom configuration
        configureHttpClient?.Invoke(httpClientBuilder);
    }

    /// <summary>
    /// Configures default resilience policies.
    /// </summary>
    private static void ConfigureDefaultResilience(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        // Retry policy
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Result?.IsSuccessStatusCode == false &&
                IsRetryableStatusCode(args.Outcome.Result?.StatusCode))
        });

        // Timeout policy
        builder.AddTimeout(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Determines if a status code should trigger a retry.
    /// </summary>
    private static bool IsRetryableStatusCode(System.Net.HttpStatusCode? statusCode)
    {
        return statusCode is
            System.Net.HttpStatusCode.TooManyRequests or
            System.Net.HttpStatusCode.RequestTimeout or
            System.Net.HttpStatusCode.InternalServerError or
            System.Net.HttpStatusCode.BadGateway or
            System.Net.HttpStatusCode.ServiceUnavailable or
            System.Net.HttpStatusCode.GatewayTimeout;
    }

}