using System;
using Kentico.Kontent.Delivery.Abstractions;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection.Tests.Integration.MultipleProjectRegistration
{
    public class ProjectAProvider : ITypeProvider
    {
        public Type GetType(string contentType) => typeof(ModelA);

        public string GetCodename(Type contentType) => "modelA";
    }
}