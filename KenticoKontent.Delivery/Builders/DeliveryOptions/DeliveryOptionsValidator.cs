using System;
using System.Text.RegularExpressions;

namespace KenticoKontent.Delivery
{
    /// <summary>
    /// A class that can be used to validate configuration of the <see cref="DeliveryOptions"/> instance.
    /// </summary>
    public static class DeliveryOptionsValidator
    {
        internal static Lazy<Regex> ApiKeyRegex = new Lazy<Regex>(() => new Regex(@"[A-Za-z0-9+/]+\.[A-Za-z0-9+/]+\.[A-Za-z0-9+/]+", RegexOptions.Compiled));

        /// <summary>
        /// Validates the <see cref="DeliveryOptions"/> instance for correct configuration, i.e, project id format, non-negative number of retry attempts,
        /// use of either Preview or Production API and whether an API key is set if the API is used.
        /// </summary>
        /// <param name="deliveryOptions">A <see cref="DeliveryOptions"/> instance.</param>
        public static void Validate(this DeliveryOptions deliveryOptions)
        {
            ValidateMaxRetryAttempts(deliveryOptions.MaxRetryAttempts);
            ValidateProjectId(deliveryOptions.ProjectId);
            ValidateUseOfPreviewAndProductionApi(deliveryOptions);
            ValidateKeyForEnabledApi(deliveryOptions);
        }

        internal static void ValidateProjectId(this string projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId), "Kentico Kontent project identifier is not specified.");
            }

            if (projectId == string.Empty)
            {
                throw new ArgumentException("Kentico Kontent project identifier is empty.", nameof(projectId));
            }

            if (!Guid.TryParse(projectId, out var projectIdGuid))
            {
                throw new ArgumentException(
                    "Provided string is not a valid project identifier ({ProjectId}). Haven't you accidentally passed the Preview API key instead of the project identifier?",
                    nameof(projectId));
            }

            ValidateProjectId(projectIdGuid);
        }

        internal static void ValidateProjectId(this Guid projectId)
        {
            if (projectId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Kentico Kontent project identifier cannot be empty UUID.",
                    nameof(projectId));
            }
        }

        internal static void ValidateApiKey(this string apiKey, string parameterName)
        {
            IsEmptyOrNull(apiKey, parameterName);

            if (!ApiKeyRegex.Value.IsMatch(apiKey))
            {
                throw new ArgumentException($"Parameter {parameterName} has invalid format.", parameterName);
            }
        }

        internal static void ValidateMaxRetryAttempts(this int attempts)
        {
            if (attempts < 0)
            {
                throw new ArgumentException("Number of maximum retry attempts can't be less than zero.", nameof(attempts));
            }
        }

        internal static void ValidateCustomEndpoint(this string customEndpoint)
        {
            IsEmptyOrNull(customEndpoint, nameof(customEndpoint));

            var canCreateUri = Uri.TryCreate(customEndpoint, UriKind.Absolute, out var uriResult);

            if (!canCreateUri)
            {
                throw new ArgumentException($"Parameter {nameof(customEndpoint)} has invalid format.", nameof(customEndpoint));
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
                throw new ArgumentException($"Parameter {nameof(customEndpoint)} has to be an absolute URI.", nameof(customEndpoint));
            }

            var hasCorrectUriScheme = customEndpoint.Scheme == Uri.UriSchemeHttp || customEndpoint.Scheme == Uri.UriSchemeHttps;

            if (!hasCorrectUriScheme)
            {
                throw new ArgumentException($"Parameter {nameof(customEndpoint)} has unsupported scheme. Please use either http or https.", nameof(customEndpoint));
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
                throw new InvalidOperationException("The Preview API key must be set while using the Preview API.");
            }

            if (deliveryOptions.UseSecuredProductionApi && string.IsNullOrWhiteSpace(deliveryOptions.SecuredProductionApiKey))
            {
                throw new InvalidOperationException("The Secured Production API key must be set while using the Secured Production API.");
            }
        }

        private static void ValidateUseOfPreviewAndProductionApi(this DeliveryOptions deliveryOptions)
        {
            if (deliveryOptions.UsePreviewApi && deliveryOptions.UseSecuredProductionApi)
            {
                throw new InvalidOperationException("Preview API and Secured Production API can't be used at the same time.");
            }
        }
    }
}
