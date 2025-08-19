using System.ComponentModel.DataAnnotations;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Kontent.Ai.Delivery.Extensions
{
    /// <summary>
    /// Extension methods for registering Kontent.ai Delivery SDK services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Kontent.ai Delivery client with the specified options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="deliveryOptions">The delivery options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDeliveryClient(
            this IServiceCollection services,
            DeliveryOptions deliveryOptions)
        {
            ArgumentNullException.ThrowIfNull(deliveryOptions);

            // Register immutable record directly - Options.Create handles both IOptions<T> and IOptionsMonitor<T>
            services.AddSingleton(Options.Create(deliveryOptions));

            // Validate the provided options using same pipeline as other overloads
            var validationContext = new ValidationContext(deliveryOptions);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(deliveryOptions, validationContext, validationResults, validateAllProperties: true))
            {
                var errors = string.Join(Environment.NewLine, validationResults.Select(r => r.ErrorMessage));
                throw new ArgumentException($"DeliveryOptions validation failed:{Environment.NewLine}{errors}");
            }

            return services.AddDeliveryCore();
        }

        /// <summary>
        /// Registers the Kontent.ai Delivery client using configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="configurationSectionName">The configuration section name. Default is "DeliveryOptions".</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDeliveryClient(
            this IServiceCollection services,
            IConfiguration configuration,
            string configurationSectionName = "DeliveryOptions")
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var section = string.IsNullOrEmpty(configurationSectionName)
                ? configuration
                : configuration.GetSection(configurationSectionName);

            // Register options with validation
            return services.AddOptions<DeliveryOptions>()
                .Bind(section)
                .ValidateDataAnnotations()
                .ValidateOnStart()
                .Services
                .AddDeliveryCore();
        }

        /// <summary>
        /// Registers the Kontent.ai Delivery client with custom configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure delivery options.</param>
        /// <param name="configureRefit">Optional action to configure Refit settings.</param>
        /// <param name="configureHttpClient">Optional action to configure the HTTP client.</param>
        /// <param name="configureResilience">Optional action to configure resilience policies.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDeliveryClient(
            this IServiceCollection services,
            Action<DeliveryOptions> configureOptions,
            Action<RefitSettings>? configureRefit = null,
            Action<IHttpClientBuilder>? configureHttpClient = null,
            Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null)
        {
            ArgumentNullException.ThrowIfNull(configureOptions);

            // Register options with validation
            return services.AddOptions<DeliveryOptions>()
                .Configure(configureOptions)
                .ValidateDataAnnotations()
                .ValidateOnStart()
                .Services
                .AddDeliveryCore(configureRefit, configureHttpClient, configureResilience);
        }

        /// <summary>
        /// Centralized method for registering Refit client, handlers, and resilience configuration.
        /// Reads all configuration from IOptionsMonitor at runtime.
        /// </summary>
        private static IServiceCollection AddDeliveryCore(
            this IServiceCollection services,
            Action<RefitSettings>? configureRefit = null,
            Action<IHttpClientBuilder>? configureHttpClient = null,
            Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null)
        {
            // Create Refit settings
            var refitSettings = RefitSettingsProvider.CreateDefaultSettings();
            configureRefit?.Invoke(refitSettings);

            // Register message handlers
            services.TryAddTransient<TrackingHandler>();
            services.TryAddTransient<DeliveryAuthenticationHandler>();

            // Register Refit client
            var httpClientBuilder = services
                .AddRefitClient<IDeliveryApi>(refitSettings)
                .ConfigureHttpClient((serviceProvider, httpClient) =>
                {
                    var options = serviceProvider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
                    var opts = options.CurrentValue;

                    // Set base address
                    var baseUrl = opts.GetBaseUrl();
                    httpClient.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/{opts.EnvironmentId}");
                })
                .AddHttpMessageHandler<TrackingHandler>()
                .AddHttpMessageHandler<DeliveryAuthenticationHandler>();

            // Allow custom HTTP client configuration
            configureHttpClient?.Invoke(httpClientBuilder);

            // Configure resilience based on runtime options
            httpClientBuilder.AddResilienceHandler("delivery-resilience", (resilienceBuilder, context) =>
            {
                // Read resilience setting from options at runtime
                var options = context.ServiceProvider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
                var opts = options.CurrentValue;

                if (!opts.EnableResilience)
                {
                    return; // Skip resilience configuration if disabled
                }

                // Default retry policy
                resilienceBuilder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                });

                // Default timeout
                resilienceBuilder.AddTimeout(TimeSpan.FromSeconds(30));

                // Default circuit breaker
                resilienceBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    FailureRatio = 0.5,
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(30)
                });

                // Allow custom resilience configuration
                configureResilience?.Invoke(resilienceBuilder);
            });

            // Register response processor and delivery client
            services.TryAddSingleton<DeliveryResponseProcessor>();
            services.TryAddSingleton<IDeliveryClient, DeliveryClient>();

            return services;
        }


    }
}