using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace KenticoKontent.Delivery.Tests.DependencyInjectionFrameworks.Helpers
{
    internal class FakeApplicationBuilder : IApplicationBuilder
    {
        public IServiceProvider ApplicationServices { get; set; }

        public IFeatureCollection ServerFeatures => throw new NotImplementedException();

        public IDictionary<String, Object> Properties => throw new NotImplementedException();

        public RequestDelegate Build() => throw new NotImplementedException();
        public IApplicationBuilder New() => throw new NotImplementedException();
        public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware) => throw new NotImplementedException();
    }
}
