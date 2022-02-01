using Kentico.Kontent.Delivery.Abstractions;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection.Tests.Integration.MultipleProjectRegistration
{
    public class ModelA
    {
        public string ModelATitle { get; set; }

        public IContentItemSystemAttributes System { get; set; }

    }
}