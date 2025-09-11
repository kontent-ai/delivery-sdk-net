using System;
using Autofac;

namespace Kontent.Ai.Delivery.Extensions.DependencyInjection;

[Obsolete("#312")]
internal class AutofacServiceProvider(IComponentContext container) : INamedServiceProvider
{
    private readonly IComponentContext _container = container;

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
