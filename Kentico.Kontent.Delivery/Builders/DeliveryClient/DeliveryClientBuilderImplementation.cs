using System;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Configuration.DeliveryOptions;
using Kentico.Kontent.Delivery.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Kontent.Delivery.Builders.DeliveryClient
{
    internal sealed class DeliveryClientBuilderImplementation : IDeliveryClientBuilder, IOptionalClientSetup
    {
        private readonly IServiceCollection _serviceCollection = new ServiceCollection();
        private DeliveryOptions _deliveryOptions;

        public IOptionalClientSetup BuildWithDeliveryOptions(Func<IDeliveryOptionsBuilder, DeliveryOptions> buildDeliveryOptions)
        {
            var builder = DeliveryOptionsBuilder.CreateInstance();
            _deliveryOptions = buildDeliveryOptions(builder);

            return this;
        }

        public IOptionalClientSetup BuildWithProjectId(string projectId)
            => BuildWithDeliveryOptions(builder =>
                builder
                    .WithProjectId(projectId)
                    .UseProductionApi()
                    .Build());

        public IOptionalClientSetup BuildWithProjectId(Guid projectId)
            => BuildWithDeliveryOptions(builder =>
                builder
                    .WithProjectId(projectId)
                    .UseProductionApi()
                    .Build());

        IOptionalClientSetup IOptionalClientSetup.WithDeliveryHttpClient(IDeliveryHttpClient  deliveryHttpClient)
            => RegisterOrThrow(deliveryHttpClient, nameof(deliveryHttpClient));

        IOptionalClientSetup IOptionalClientSetup.WithContentLinkUrlResolver(IContentLinkUrlResolver contentLinkUrlResolver)
            => RegisterOrThrow(contentLinkUrlResolver, nameof(contentLinkUrlResolver));

        IOptionalClientSetup IOptionalClientSetup.WithInlineContentItemsResolver<T>(IInlineContentItemsResolver<T> inlineContentItemsResolver)
            => RegisterInlineContentItemsResolverOrThrow(inlineContentItemsResolver);

        IOptionalClientSetup IOptionalClientSetup.WithInlineContentItemsProcessor(IInlineContentItemsProcessor inlineContentItemsProcessor)
            => RegisterOrThrow(inlineContentItemsProcessor, nameof(inlineContentItemsProcessor));

        IOptionalClientSetup IOptionalClientSetup.WithModelProvider(IModelProvider modelProvider)
            => RegisterOrThrow(modelProvider, nameof(modelProvider));

        IOptionalClientSetup IOptionalClientSetup.WithTypeProvider(ITypeProvider typeProvider)
            => RegisterOrThrow(typeProvider, nameof(typeProvider));

        IOptionalClientSetup IOptionalClientSetup.WithRetryPolicyProvider(IRetryPolicyProvider retryPolicyProvider)
            => RegisterOrThrow(retryPolicyProvider, nameof(retryPolicyProvider));

        IOptionalClientSetup IOptionalClientSetup.WithPropertyMapper(IPropertyMapper propertyMapper)
            => RegisterOrThrow(propertyMapper, nameof(propertyMapper));

        IDeliveryClient IDeliveryClientBuild.Build()
        {
            _serviceCollection.AddDeliveryClient(_deliveryOptions);
            var serviceProvider = _serviceCollection.BuildServiceProvider();
            var client = serviceProvider.GetService<IDeliveryClient>();

            return client;
        }

        private DeliveryClientBuilderImplementation RegisterOrThrow<TType>(TType instance, string parameterName) where TType : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            _serviceCollection.AddSingleton(instance);

            return this;
        }

        private DeliveryClientBuilderImplementation RegisterInlineContentItemsResolverOrThrow<TContentItem>(IInlineContentItemsResolver<TContentItem> inlineContentItemsResolver)
        {
            if (inlineContentItemsResolver == null)
            {
                throw new ArgumentNullException(nameof(inlineContentItemsResolver));
            }

            _serviceCollection.AddDeliveryInlineContentItemsResolver(inlineContentItemsResolver);

            return this;
        }
    }
}
