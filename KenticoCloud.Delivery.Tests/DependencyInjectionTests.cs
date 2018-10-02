using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FakeItEasy;
using KenticoCloud.Delivery.InlineContentItems;

namespace KenticoCloud.Delivery.Tests
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void InstantiationTest()
        {
            // Arrange
            var cfmp = A.Fake<ICodeFirstModelProvider>();
            var clur = A.Fake<IContentLinkUrlResolver>();
            var cip = A.Fake<IInlineContentItemsProcessor>();
            var projectId = Guid.NewGuid().ToString();

            // Act
            var serviceProvider = new ServiceCollection()
                .AddScoped(c => cfmp)
                .AddScoped(c => clur)
                .AddScoped(c => cip)
                .AddOptions()
                .Configure<DeliveryOptions>(o => o.ProjectId = projectId)
                .AddScoped<IDeliveryClient, DeliveryClient>()
                .BuildServiceProvider();

            IDeliveryClient dc = serviceProvider.GetService<IDeliveryClient>();

            // Assert
            // Assert.Equal(cfmp, dc.CodeFirstModelProvider);
            // Assert.Equal(clur, dc.ContentLinkUrlResolver);
            // Assert.Equal(cip, dc.InlineContentItemsProcessor);
        }
    }
}
