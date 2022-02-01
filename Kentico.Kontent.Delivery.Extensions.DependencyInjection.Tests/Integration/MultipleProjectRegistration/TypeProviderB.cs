using System;
using Kentico.Kontent.Delivery.Abstractions;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection.Tests.Integration.MultipleProjectRegistration
{
    public class ProjectBProvider : ITypeProvider
    {
        public Type GetType(string contentType) => typeof(ModelB);

        public string GetCodename(Type contentType) => "modelB";
    }
}