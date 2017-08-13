using System;
using KenticoCloud.Delivery;

namespace KenticoCloud.Delivery.Tests
{
    public class CustomTypeProvider : ICodeFirstTypeProvider
    {
        public Type GetType(string contentType)
        {
            switch (contentType)
            {
                case "about_us":
                    return typeof(AboutUs);
                case "accessory":
                    return typeof(Accessory);
                case "article":
                    return typeof(Article);
                case "brewer":
                    return typeof(Brewer);
                case "cafe":
                    return typeof(Cafe);
                case "coffee":
                    return typeof(Coffee);
                case "fact_about_us":
                    return typeof(FactAboutUs);
                case "grinder":
                    return typeof(Grinder);
                case "hero_unit":
                    return typeof(HeroUnit);
                case "home":
                    return typeof(Home);
                case "hosted_video":
                    return typeof(HostedVideo);
                case "office":
                    return typeof(Office);
                case "tweet":
                    return typeof(Tweet);
                default:
                    return null;
            }
        }
    }
}