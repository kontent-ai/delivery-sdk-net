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
            // You can create your own, or generate it automatically
            public string Title { get; set; }
        }

        // You can create your own, or generate it automatically
        public class TheatreTypeProvider : ITypeProvider
        {
            public Type GetType(string contentType)
            => contentType == "movie" ? typeof(Movie) : null;
            public string GetCodename(Type contentType)
            => contentType == typeof(Movie) ? "movie" : null;
        }

        [Fact]
        public async void GetMoviesTest()
        {
            var client = DeliveryClientBuilder.WithProjectId("975bf280-fd91-488c-994c-2f04416e5ee3")
                .WithTypeProvider(new TheatreTypeProvider())
                .Build();

            var response = await client.GetItemsAsync<Movie>();

            var itemTitles = response.Items.Select(item => item.Title);
        }


    }

}