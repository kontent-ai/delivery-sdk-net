using System;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Builders.DeliveryClient
{
    /// <summary>
    /// A builder abstraction of mandatory setup of <see cref="IDeliveryClient"/> instances.
    /// </summary>
    public interface IDeliveryClientBuilder
    {
        /// <seealso cref="DeliveryClientBuilder.WithEnvironmentId(string)"/>>
        IOptionalClientSetup BuildWithEnvironmentId(string environmentId);

        /// <seealso cref="DeliveryClientBuilder.WithEnvironmentId(Guid)"/>>
        IOptionalClientSetup BuildWithEnvironmentId(Guid environmentId);

        /// <seealso cref="DeliveryClientBuilder.WithOptions(Func{IDeliveryOptionsBuilder, DeliveryOptions})"/>
        IOptionalClientSetup BuildWithDeliveryOptions(Func<IDeliveryOptionsBuilder, DeliveryOptions> buildDeliveryOptions);
    }

    /// <summary>
    /// A builder abstraction of optional setup of <see cref="IDeliveryClient"/> instances.
    /// </summary>
    public interface IOptionalClientSetup : IDeliveryClientBuild
    {
        /// <summary>
        /// Use a custom delivery HTTP client
        /// </summary>
        /// <param name="deliveryHttpClient">A custom <see cref="IDeliveryHttpClient"/> implementation</param>
        IOptionalClientSetup WithDeliveryHttpClient(IDeliveryHttpClient deliveryHttpClient);

        /// <summary>
        /// Use a custom object to provide URL for content links in rich text elements.
        /// </summary>
        /// <param name="contentLinkUrlResolver">An instance of the <see cref="IContentLinkUrlResolver"/>.</param>
        IOptionalClientSetup WithContentLinkUrlResolver(IContentLinkUrlResolver contentLinkUrlResolver);

        /// <summary>
        /// Use an object to transform linked items and components of the specified type in rich text elements to a valid HTML fragment.
        /// </summary>
        /// <typeparam name="T">The type of the linked item or component to transform.</typeparam>
        /// <param name="inlineContentItemsResolver">An instance of the <see cref="IInlineContentItemsResolver{T}"/>.</param>
        IOptionalClientSetup WithInlineContentItemsResolver<T>(IInlineContentItemsResolver<T> inlineContentItemsResolver);

        /// <summary>
        /// Use a custom object to transform HTML content of rich text elements.
        /// </summary>
        /// <param name="inlineContentItemsProcessor">An instance of the <see cref="IInlineContentItemsProcessor"/>.</param>
        IOptionalClientSetup WithInlineContentItemsProcessor(IInlineContentItemsProcessor inlineContentItemsProcessor);

        /// <summary>
        /// Use a custom provider to convert JSON data into objects.
        /// </summary>
        /// <param name="modelProvider">An instance of the <see cref="IModelProvider"/>.</param>
        IOptionalClientSetup WithModelProvider(IModelProvider modelProvider);

        /// <summary>
        /// Use a custom provider to map content type codenames to content type objects.
        /// </summary>
        /// <param name="typeProvider">An instance of the <see cref="ITypeProvider"/>.</param>
        IOptionalClientSetup WithTypeProvider(ITypeProvider typeProvider);

        /// <summary>
        /// Use a custom provider to create retry polices for HTTP requests.
        /// </summary>
        /// <param name="retryPolicyProvider">An instance of the <see cref="IRetryPolicyProvider"/>.</param>
        IOptionalClientSetup WithRetryPolicyProvider(IRetryPolicyProvider retryPolicyProvider);

        /// <summary>
        /// Use a custom mapper to determine relationships between elements of a content item and properties of a model that represents this item.
        /// </summary>
        /// <param name="propertyMapper">An instance of the <see cref="IPropertyMapper"/>.</param>
        IOptionalClientSetup WithPropertyMapper(IPropertyMapper propertyMapper);

        /// <summary>
        /// Use a ILoggerFactory to allow logging from <see cref="IDeliveryClient"/>
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        IOptionalClientSetup WithLoggerFactory(ILoggerFactory loggerFactory);
    }

    /// <summary>
    /// A builder abstraction of the last step in the setup of <see cref="IDeliveryClient"/> instances.
    /// </summary>
    public interface IDeliveryClientBuild
    {
        /// <summary>
        /// Returns a new instance of the <see cref="IDeliveryClient"/>.
        /// </summary>
        IDeliveryClient Build();
    }
}
