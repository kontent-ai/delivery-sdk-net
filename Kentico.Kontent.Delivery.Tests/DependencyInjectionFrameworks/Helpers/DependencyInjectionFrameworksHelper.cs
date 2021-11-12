﻿using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Castle.Windsor;
using Castle.Windsor.MsDependencyInjection;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.Configuration;
using Kentico.Kontent.Delivery.Extensions;
using Kentico.Kontent.Delivery.Tests.Factories;
using Kentico.Kontent.Delivery.Tests.Models.ContentTypes;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using Unity;
using Unity.Microsoft.DependencyInjection;

namespace Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks.Helpers
{
    internal static class DependencyInjectionFrameworksHelper
    {
        private const string ProjectId = "00a21be4-8fef-4dd9-9380-f4cbb82e260d";

        internal static IServiceCollection GetServiceCollection()
            => new ServiceCollection()
                .AddDeliveryClient(new DeliveryOptions { ProjectId = ProjectId });

        internal static IServiceCollection RegisterInlineContentItemResolvers(this IServiceCollection serviceCollection)
            => serviceCollection
                .AddDeliveryInlineContentItemsResolver(InlineContentItemsResolverFactory.Instance.ResolveToMessage<HostedVideo>(string.Empty))
                .AddDeliveryInlineContentItemsResolver<Tweet, FakeTweetResolver>();

        internal static IServiceProvider BuildAutoFacServiceProvider(this IServiceCollection serviceCollection)
        {
            var autoFacContainerBuilder = new ContainerBuilder();
            autoFacContainerBuilder.Populate(serviceCollection);

            var container = autoFacContainerBuilder.Build();

            return new AutofacServiceProvider(container);
        }

        internal static IServiceProvider BuildWindsorCastleServiceProvider(this IServiceCollection serviceCollection)
        {
            var castleContainer = new WindsorContainer();

            return WindsorRegistrationHelper.CreateServiceProvider(castleContainer, serviceCollection);
        }

        internal static IServiceProvider BuildUnityServiceProvider(this IServiceCollection serviceCollection)
        {
            var unityContainer = new UnityContainer();

            return serviceCollection.BuildServiceProvider(unityContainer);
        }

        internal static Container BuildSimpleInjectorServiceProvider(this IServiceCollection serviceCollection)
        {
            var container = new Container
            {
                Options =
                {
                    DefaultScopedLifestyle = new AsyncScopedLifestyle()
                }
            };

            serviceCollection.BuildServiceProvider();

            serviceCollection.AddSimpleInjector(container);
            serviceCollection.UseSimpleInjectorAspNetRequestScoping(container);

            ServiceCollection services = serviceCollection as ServiceCollection;

            ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services, true)
                .UseSimpleInjector(container);



            return container;
        }
    }
}
