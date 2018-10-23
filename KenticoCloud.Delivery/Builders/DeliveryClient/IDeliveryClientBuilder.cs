using System;
using System.Net.Http;
using KenticoCloud.Delivery.Builders.DeliveryOptions;
using KenticoCloud.Delivery.InlineContentItems;
using KenticoCloud.Delivery.ResiliencePolicy;

namespace KenticoCloud.Delivery.Builders.DeliveryClient
{
    /// <summary>
    /// Defines the contracts of the mandatory steps for building a Kentico Cloud <see cref="IDeliveryClient"/> instance.
    /// </summary>
    public interface IDeliveryClientBuilder
    {
        /// <seealso cref="DeliveryClientBuilder.WithProjectId(string)"/>>
        IOptionalClientSetup BuildWithProjectId(string projectId);

        /// <seealso cref="DeliveryClientBuilder.WithProjectId(Guid)"/>>
        IOptionalClientSetup BuildWithProjectId(Guid projectId);

        /// <seealso cref="DeliveryClientBuilder.WithOptions(Func{IDeliveryOptionsBuilder, Delivery.DeliveryOptions})"/>
        IOptionalClientSetup BuildWithDeliveryOptions(Func<IDeliveryOptionsBuilder, Delivery.DeliveryOptions> buildDeliveryOptions);
    }

    /// <summary>
    /// Defines the contracts of the optional steps for building a Kentico Cloud <see cref="IDeliveryClient"/> instance.
    /// </summary>
    public interface IOptionalClientSetup : IDeliveryClientBuild
    {
        /// <summary>
        /// Sets a custom HTTP client instance to the <see cref="IDeliveryClient"/> instance.
        /// </summary>
        /// <param name="httpClient">A custom HTTP client instance</param>
        IOptionalClientSetup WithHttpClient(HttpClient httpClient);

        /// <summary>
        /// Sets a custom instance of an object that can resolve links in rich text elements to the <see cref="IDeliveryClient"/> instance.
        /// </summary>
        /// <param name="contentLinkUrlResolver">An instance of an object that can resolve links in rich text elements</param>
        IOptionalClientSetup WithContentLinkUrlResolver(IContentLinkUrlResolver contentLinkUrlResolver);

        /// <summary>
        /// Sets a custom instance of an object that can resolve specific content type of an inline content item to the <see cref="IDeliveryClient"/> instance.
        /// </summary>
        /// <typeparam name="T">Content type to be resolved</typeparam>
        /// <param name="inlineContentItemsResolver">An instance of an object that can resolve component and linked items to HTML markup</param>
        /// <returns></returns>
        IOptionalClientSetup WithInlineContentItemsResolver<T>(IInlineContentItemsResolver<T> inlineContentItemsResolver);

        /// <summary>
        /// Sets a custom instance of an object that can resolve modular content in rich text elements to the <see cref="IDeliveryClient"/> instance.
        /// </summary>
        /// <param name="inlineContentItemsProcessor">An instance of an object that can resolve modular content in rich text elements</param>
        IOptionalClientSetup WithInlineContentItemsProcessor(IInlineContentItemsProcessor inlineContentItemsProcessor);

        /// <summary>
        /// Sets a custom instance of an object that can JSON responses into strongly typed CLR objects to the <see cref="IDeliveryClient"/> instance.
        /// </summary>
        /// <param name="codeFirstModelProvider">An instance of an object that can JSON responses into strongly typed CLR objects</param>
        IOptionalClientSetup WithCodeFirstModelProvider(ICodeFirstModelProvider codeFirstModelProvider);

        /// <summary>
        /// Sets a custom instance of an object that can map Kentico Cloud content types to CLR types to the <see cref="IDeliveryClient"/> instance.
        /// </summary>
        /// <param name="codeFirstTypeProvider">An instance of an object that can map Kentico Cloud content types to CLR types</param>
        IOptionalClientSetup WithCodeFirstTypeProvider(ICodeFirstTypeProvider codeFirstTypeProvider);

        /// <summary>
        /// Sets a custom instance of a provider of a resilience (retry) policy to the <see cref="IDeliveryClient"/> instance.
        /// </summary>
        /// <param name="resiliencePolicyProvider">A provider of a resilience (retry) policy</param>
        IOptionalClientSetup WithResiliencePolicyProvider(IResiliencePolicyProvider resiliencePolicyProvider);

        /// <summary>
        /// Sets a custom instance of an object that can map Kentico Cloud content item fields to model properties to the <see cref="IDeliveryClient"/> instance.
        /// </summary>
        /// <param name="propertyMapper">An instance of an object that can map Kentico Cloud content item fields to model properties</param>
        IOptionalClientSetup WithCodeFirstPropertyMapper(ICodeFirstPropertyMapper propertyMapper);
    }

    /// <summary>
    /// Defines the contract of the last build step that initializes a new instance of the <see cref="IDeliveryClient"/> interface.
    /// </summary>
    public interface IDeliveryClientBuild
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IDeliveryClient"/> interface for retrieving content of the specified project.
        /// </summary>
        IDeliveryClient Build();
    }
}
