using System;
using System.Collections.Generic;
using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentItems;

namespace Kentico.Kontent.Delivery.Benchmarks.ContentTypes
{
    public class CustomTypeProvider : ITypeProvider
    {
        private static readonly Dictionary<Type, string> _codenames = new Dictionary<Type, string>
        {
            {typeof(AboutUs), "about_us"},
            {typeof(Accessory), "accessory"},
            {typeof(Article), "article"},
            {typeof(Brewer), "brewer"},
            {typeof(Cafe), "cafe"},
            {typeof(Coffee), "coffee"},
            {typeof(FactAboutUs), "fact_about_us"},
            {typeof(Grinder), "grinder"},
            {typeof(HeroUnit), "hero_unit"},
            {typeof(Home), "home"},
            {typeof(HostedVideo), "hosted_video"},
            {typeof(Office), "office"},
            {typeof(Tweet), "tweet"}
        };

        public Type GetType(string contentType)
        {
            return _codenames.Keys.FirstOrDefault(type => GetCodename(type).Equals(contentType));
        }

        public string GetCodename(Type contentType)
        {
            return _codenames.TryGetValue(contentType, out var codename) ? codename : null;
        }
    }
}