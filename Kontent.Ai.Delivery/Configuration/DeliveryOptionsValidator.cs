using System;
using System.Text.RegularExpressions;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Configuration
{
    /// <summary>
    /// Validates instances of the <see cref="DeliveryOptions"/> class.
    /// </summary>
    public static class DeliveryOptionsValidator
    {
        internal static Lazy<Regex> ApiKeyRegex = new Lazy<Regex>(() => new Regex(@"[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+", RegexOptions.Compiled));

        /// <summary>
        /// Validates an instance of the <see cref="DeliveryOptions"/> class if it is compatible with <see cref="DeliveryClient"/>.
        /// If the configuration is not valid, an exception is thrown.
        /// </summary>
        /// <param name="deliveryOptions">An instance of the <see cref="DeliveryOptions"/> class.</param>
        public static void Validate(this DeliveryOptions deliveryOptions)
        {
            ValidateProjectId(deliveryOptions.ProjectId);
            ValidateUseOfPreviewAndProductionApi(deliveryOptions);
            ValidateKeyForEnabledApi(deliveryOptions);
            ValidateRetryPolicyOptions(deliveryOptions.DefaultRetryPolicyOptions);
        }

        internal static void ValidateProjectId(this string projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId), "Kontent.ai project identifier is not specified.");
            }

            if (projectId == string.Empty)
            {
                throw new ArgumentException("Kontent.ai project identifier is empty.", nameof(projectId));
            }

            if (!Guid.TryParse(projectId, out var projectIdGuid))
            {
                throw new ArgumentException("Kontent.ai project identifier '{ProjectId}' is not valid. Perhaps you have passed an API key instead?", nameof(projectId));
            }

            ValidateProjectId(projectIdGuid);
        }

        internal static void ValidateProjectId(this Guid projectId)
        {
            if (projectId == Guid.Empty)
            {
                throw new ArgumentException("Kontent.ai project identifier is an empty GUID.", nameof(projectId));
            }
        }

        internal static void ValidateApiKey(this string apiKey, string parameterName)
        {
            IsEmptyOrNull(apiKey, parameterName);

            if (!ApiKeyRegex.Value.IsMatch(apiKey))
            {
                throw new ArgumentException($"Parameter {parameterName} is not an API key.", parameterName);
            }
        }

        internal static void ValidateRetryPolicyOptions(this DefaultRetryPolicyOptions retryPolicyOptions)
        {
            if (retryPolicyOptions == null)
            {
                throw new ArgumentNullException(nameof(retryPolicyOptions), $"Parameter {nameof(retryPolicyOptions)} is not specified.");
            }

            if (retryPolicyOptions.DeltaBackoff <= TimeSpan.Zero)
            {
                throw new ArgumentException($"Parameter {nameof(retryPolicyOptions.DeltaBackoff)} must be a positive timespan.");
            }

            if (retryPolicyOptions.MaxCumulativeWaitTime <= TimeSpan.Zero)
            {
                throw new ArgumentException($"Parameter {nameof(retryPolicyOptions.MaxCumulativeWaitTime)} must be a positive timespan.");
            }
        }

        internal static void ValidateCustomEndpoint(this string customEndpoint)
        {
            IsEmptyOrNull(customEndpoint, nameof(customEndpoint));

            var canCreateUri = Uri.TryCreate(customEndpoint, UriKind.Absolute, out var uriResult);

            if (!canCreateUri)
            {
                throw new ArgumentException($"Parameter {nameof(customEndpoint)} is not a valid URL.", nameof(customEndpoint));
            }

            ValidateCustomEndpoint(uriResult);
        }

        internal static void ValidateCustomEndpoint(this Uri customEndpoint)
        {
            if (customEndpoint == null)
            {
                throw new ArgumentNullException(nameof(customEndpoint), $"Parameter {nameof(customEndpoint)} is not specified");
            }

            if (!customEndpoint.IsAbsoluteUri)
            {
                throw new ArgumentException($"Parameter {nameof(customEndpoint)} is not an absolute URL.", nameof(customEndpoint));
            }

            var hasCorrectUriScheme = customEndpoint.Scheme == Uri.UriSchemeHttp || customEndpoint.Scheme == Uri.UriSchemeHttps;

            if (!hasCorrectUriScheme)
            {
                throw new ArgumentException($"Parameter {nameof(customEndpoint)} has scheme that is not supported. Please use either HTTP or HTTPS.", nameof(customEndpoint));
            }
        }

        private static void IsEmptyOrNull(this string value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName, $"Parameter {parameterName} is not specified.");
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Parameter {parameterName} is empty.", parameterName);
            }
        }

        private static void ValidateKeyForEnabledApi(this DeliveryOptions deliveryOptions)
        {
            if (deliveryOptions.UsePreviewApi && string.IsNullOrWhiteSpace(deliveryOptions.PreviewApiKey))
            {
                throw new InvalidOperationException("The Preview API key must be set to be able to retrieve content with the Preview API.");
            }

            if (deliveryOptions.UseSecureAccess && string.IsNullOrWhiteSpace(deliveryOptions.SecureAccessApiKey))
            {
                throw new InvalidOperationException("The secure access API key must be set to be able to retrieve content with Production API when secure access is enabled.");
            }
        }

        private static void ValidateUseOfPreviewAndProductionApi(this DeliveryOptions deliveryOptions)
        {
            if (deliveryOptions.UsePreviewApi && deliveryOptions.UseSecureAccess)
            {
                throw new InvalidOperationException("Preview API and Production API with secured access enabled can't be used at the same time.");
            }
        }
    }
}
