using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Builders.DeliveryClient;
using Xunit;

namespace Kontent.Ai.Delivery.Tests
{
    public class OndrejPDemoRollup
    {

        public partial class Movie
        {
            public const string Codename = "hero_unit";
            public const string TitleCodename = "title";
            public string Title { get; set; }
        }

        public class TheatreTypeProvider : ITypeProvider
        {
            private static readonly Dictionary<Type, string> _codenames = new Dictionary<Type, string>
            {
                {typeof(Movie), "about_us"},
                // ...
            };

            public Type GetType(string contentType)
            => _codenames.Keys.FirstOrDefault(type => GetCodename(type).Equals(contentType));
            public string GetCodename(Type contentType)
            => _codenames.TryGetValue(contentType, out var codename) ? codename : null;
        }

        [Fact]
        public async void GetMoviesTest()
        {
            var client = DeliveryClientBuilder.WithProjectId("975bf280-fd91-488c-994c-2f04416e5ee3")
                .WithTypeProvider(new TheatreTypeProvider())
                .Build();
            
            var response = await client.GetItemsAsync<Movie>();

            var items = response.Items;
        }


    }

}