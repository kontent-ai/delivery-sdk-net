using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kentico.Kontent.Delivery.Caching.Tests.Factories
{
    public class DeliveryCacheManagerFactoryTests
    {
        private readonly IOptionsMonitor<DeliveryCacheManagerFactoryOptions> _deliveryCacheManagerFactoryOptionsMock;
        private readonly ServiceCollection _serviceCollection;

        private const string _clientName = "ClientName";

        public DeliveryCacheManagerFactoryTests()
        {
            _deliveryCacheManagerFactoryOptionsMock = A.Fake<IOptionsMonitor<DeliveryCacheManagerFactoryOptions>>();
            _serviceCollection = new ServiceCollection();
        }

        [Theory]
        [InlineData(CacheTypeEnum.Memory)]
        [InlineData(CacheTypeEnum.Distributed)]
        public void GetNamedDeliveryCacheManager_WithCorrectName_GetDeliveryCacheManager(CacheTypeEnum cacheType)
        {
            _serviceCollection.AddMemoryCache();
            _serviceCollection.AddDistributedMemoryCache();

            var deliveryOptions = new DeliveryCacheOptions
            {
                CacheType = cacheType
            };

            var deliveryCacheManagerFactoryOptions = new DeliveryCacheManagerFactoryOptions();
            deliveryCacheManagerFactoryOptions.DeliveryCacheOptions.Add(() => deliveryOptions);

            A.CallTo(() => _deliveryCacheManagerFactoryOptionsMock.Get(_clientName))
                .Returns(deliveryCacheManagerFactoryOptions);

            var deliveryCacheManagerFactory = new DeliveryCacheManagerFactory(_deliveryCacheManagerFactoryOptionsMock, _serviceCollection.BuildServiceProvider());

            var result = deliveryCacheManagerFactory.Get(_clientName);

            result.Should().NotBeNull();
        }

        [Theory]
        [InlineData(CacheTypeEnum.Memory)]
        [InlineData(CacheTypeEnum.Distributed)]
        public void GetNamedDeliveryCacheManager_WithCorrectName_GetNull(CacheTypeEnum cacheType)
        {
            var deliveryOptions = new DeliveryCacheOptions
            {
                CacheType = cacheType
            };

            var deliveryCacheManagerFactoryOptions = new DeliveryCacheManagerFactoryOptions();
            deliveryCacheManagerFactoryOptions.DeliveryCacheOptions.Add(() => deliveryOptions);

            A.CallTo(() => _deliveryCacheManagerFactoryOptionsMock.Get(_clientName))
                .Returns(deliveryCacheManagerFactoryOptions);

            var deliveryCacheManagerFactory = new DeliveryCacheManagerFactory(_deliveryCacheManagerFactoryOptionsMock, _serviceCollection.BuildServiceProvider());

            var result = deliveryCacheManagerFactory.Get("WrongName");

            result.Should().BeNull();
        }
    }
}
