﻿using Autofac;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection.NamedServiceProviders
{
    internal class AutofacServiceProvider : INamedServiceProvider
    {
        private readonly IComponentContext _container;

        public AutofacServiceProvider(IComponentContext container)
        {
            _container = container;
        }

        public T GetService<T>(string name)
        {
            try
            {
                return _container.ResolveNamed<T>(name);
            }
            catch
            {
                return default(T);
            }
           
        }
    }
}
