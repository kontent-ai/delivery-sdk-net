using Kentico.Kontent.Delivery.Abstractions;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection.Tests.Integration.MultipleProjectRegistration
{
    public class ModelB
    {
        public string ModelBTitle { get; set; }

        public IContentItemSystemAttributes System { get; set; }
    }
}