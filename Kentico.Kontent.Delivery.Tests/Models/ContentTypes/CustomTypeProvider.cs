using System;
using System.Collections.Generic;
using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;

namespace Kentico.Kontent.Delivery.Tests.Models.ContentTypes
{
    public class CustomTypeProvider : ITypeProvider
    {
        private static readonly Dictionary<Type, string> TypesDictionary = new Dictionary<Type, string>
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
            {typeof(Tweet), "tweet"},
            {typeof(SimpleRichText), "simple_rich_text" }
        };

        public Type GetType(string contentType)
        {
            return TypesDictionary.Keys.FirstOrDefault(type => GetCodename(type).Equals(contentType));
        }

        public string GetCodename(Type contentType)
        {
            return TypesDictionary.TryGetValue(contentType, out var codename) ? codename : null;
        }
    }
}