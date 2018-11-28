using KenticoCloud.Delivery.Builders.DeliveryOptions;
using KenticoCloud.Delivery.Extensions;
using KenticoCloud.Delivery.InlineContentItems;
using KenticoCloud.Delivery.ResiliencePolicy;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace KenticoCloud.Delivery.Builders.DeliveryClient
{
    internal sealed class DeliveryClientBuilderImplementation : IDeliveryClientBuilder, IOptionalClientSetup
    {
        private readonly IServiceCollection _serviceCollection = new ServiceCollection();
        private Delivery.DeliveryOptions _deliveryOptions;
        private IInlineContentItemsResolverCollection _inlineContentItemsResolvers = new InlineContentItemsResolverCollection();

        public IOptionalClientSetup BuildWithDeliveryOptions(Func<IDeliveryOptionsBuilder, Delivery.DeliveryOptions> buildDeliveryOptions)
        {
            var builder = DeliveryOptionsBuilder.CreateInstance();

            _deliveryOptions = buildDeliveryOptions(builder);

            return this;
        }

        public IOptionalClientSetup BuildWithProjectId(string projectId)
            => BuildWithDeliveryOptions(builder =>
                builder
                    .WithProjectId(projectId)
                    .UseProductionApi
                    .Build());

        public IOptionalClientSetup BuildWithProjectId(Guid projectId)
            => BuildWithDeliveryOptions(builder =>
                builder
                    .WithProjectId(projectId)
                    .UseProductionApi
                    .Build());

        IOptionalClientSetup IOptionalClientSetup.WithHttpClient(HttpClient httpClient)
            => RegisterOrThrow(httpClient, nameof(httpClient));

        IOptionalClientSetup IOptionalClientSetup.WithContentLinkUrlResolver(IContentLinkUrlResolver contentLinkUrlResolver)
            => RegisterOrThrow(contentLinkUrlResolver, nameof(contentLinkUrlResolver));

        IOptionalClientSetup IOptionalClientSetup.WithInlineContentItemsResolver<T>(IInlineContentItemsResolver<T> inlineContentItemsResolver)
            => RegisterTypeResolver(inlineContentItemsResolver);

        IOptionalClientSetup IOptionalClientSetup.WithInlineContentItemsProcessor(IInlineContentItemsProcessor inlineContentItemsProcessor)
            => RegisterOrThrow(inlineContentItemsProcessor, nameof(inlineContentItemsProcessor));

        IOptionalClientSetup IOptionalClientSetup.WithCodeFirstModelProvider(ICodeFirstModelProvider codeFirstModelProvider)
            => RegisterOrThrow(codeFirstModelProvider, nameof(codeFirstModelProvider));

        IOptionalClientSetup IOptionalClientSetup.WithCodeFirstTypeProvider(ICodeFirstTypeProvider codeFirstTypeProvider)
            => RegisterOrThrow(codeFirstTypeProvider, nameof(codeFirstTypeProvider));

        IOptionalClientSetup IOptionalClientSetup.WithResiliencePolicyProvider(IResiliencePolicyProvider resiliencePolicyProvider)
            => RegisterOrThrow(resiliencePolicyProvider, nameof(resiliencePolicyProvider));

        IOptionalClientSetup IOptionalClientSetup.WithCodeFirstPropertyMapper(ICodeFirstPropertyMapper propertyMapper)
            => RegisterOrThrow(propertyMapper, nameof(propertyMapper));

        IDeliveryClient IDeliveryClientBuild.Build()
        {
            _serviceCollection.AddSingleton(typeof(IInlineContentItemsResolverCollection), _inlineContentItemsResolvers);
            _serviceCollection.AddDeliveryClient(_deliveryOptions);

            var serviceProvider = _serviceCollection.BuildServiceProvider();

            var client = serviceProvider.GetService<IDeliveryClient>();

            return client;
        }

        private DeliveryClientBuilderImplementation RegisterContentItemResolver<T>(IInlineContentItemsResolver<T> inlineContentItemsResolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            _inlineContentItemsResolvers.InlineContentItemsResolvers.RegisterTypeResolver(resolver);
            return this;
        }

        private DeliveryClientBuilderImplementation RegisterOrThrow<TType>(TType instance, string parameterName)
            where TType : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            _serviceCollection.AddSingleton(instance);

            return this;
        }
    }
}
