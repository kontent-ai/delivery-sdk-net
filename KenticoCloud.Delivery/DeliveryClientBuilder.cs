using KenticoCloud.Delivery.InlineContentItems;
using KenticoCloud.Delivery.ResiliencePolicy;
using Microsoft.Extensions.Options;

namespace KenticoCloud.Delivery
{
    public class DeliveryClientBuilder
    {
        private IContentLinkUrlResolver _contentLinkUrlResolver;
        private IInlineContentItemsProcessor _inlineContentItemsProcessor;
        private ICodeFirstModelProvider _codeFirstModelProvider;
        private IResiliencePolicyProvider _resiliencePolicyProvider;
        private ICodeFirstTypeProvider _codeFirstTypeProvider;
        private DeliveryOptions _deliveryOptions;
        private string _projectId;
        private string _previewApiKey;

        public DeliveryClientBuilder WithProjectId(string projectId)
        {
            _projectId = projectId;

            return this;
        }

        public DeliveryClientBuilder WithPreviewApiKey(string previewApiKey)
        {
            _previewApiKey = previewApiKey;

            return this;
        }

        public DeliveryClientBuilder WithDeliveryOptions(DeliveryOptions deliveryOptions)
        {
            _deliveryOptions = deliveryOptions;

            return this;
        }

        public DeliveryClientBuilder WithContentLinkUrlResolver(IContentLinkUrlResolver contentLinkUrlResolver)
        {
            _contentLinkUrlResolver = contentLinkUrlResolver;

            return this;
        }

        public DeliveryClientBuilder WithInlineContentItemsProcessor(
            IInlineContentItemsProcessor inlineContentItemsProcessor)
        {
            _inlineContentItemsProcessor = inlineContentItemsProcessor;

            return this;
        }

        public DeliveryClientBuilder WithCodeFirstModelProvider(ICodeFirstModelProvider codeFirstModelProvider, ICodeFirstTypeProvider codeFirstTypeProvider)
        {
            _codeFirstModelProvider = codeFirstModelProvider;
            _codeFirstTypeProvider = codeFirstTypeProvider;

            return this;
        }

        public DeliveryClientBuilder WithResiliencePolicyProvider(IResiliencePolicyProvider resiliencePolicyProvider)
        {
            _resiliencePolicyProvider = resiliencePolicyProvider;

            return this;
        }

        public DeliveryClient Build()
        {
            OptionsWrapper<DeliveryOptions> deliveryOptions;

            if (_deliveryOptions == null)
            {
                deliveryOptions =
                    new OptionsWrapper<DeliveryOptions>(new DeliveryOptions
                    {
                        ProjectId = _projectId,
                        PreviewApiKey = _previewApiKey
                    });
            }
            else
            {
                deliveryOptions = new OptionsWrapper<DeliveryOptions>(_deliveryOptions);
            }

            var inlineContentItemsProcessor =
                _inlineContentItemsProcessor ??
                new InlineContentItemsProcessor(
                    new ReplaceWithWarningAboutRegistrationResolver(),
                    new ReplaceWithWarningAboutUnretrievedItemResolver()
                );
            var codeFirstModelProvider =
                _codeFirstModelProvider ??
                new CodeFirstModelProvider(
                    _contentLinkUrlResolver,
                    inlineContentItemsProcessor
                );

            codeFirstModelProvider.TypeProvider = _codeFirstTypeProvider;

            return new DeliveryClient(
                deliveryOptions,
                _contentLinkUrlResolver,
                inlineContentItemsProcessor,
                codeFirstModelProvider,
                _resiliencePolicyProvider
            );
        }
    }
}