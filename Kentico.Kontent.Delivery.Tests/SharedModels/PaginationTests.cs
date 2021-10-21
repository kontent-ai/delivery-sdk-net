using Kentico.Kontent.Delivery.Caching.Extensions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;
using System.Linq;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.SharedModels
{
    public class PaginationTests
    {


        [Fact]
        public void SerialisePagination_AndThen_DeserialiseToCheck_ValuesMatch_UsingStandardSerialisation()
        {

            var pagination = new Pagination(2, 500, 10, 65, "https://nextpageUrl");
            var json = JsonConvert.SerializeObject(pagination);

            var deserialisedPagination = JsonConvert.DeserializeObject<Pagination>(json);

            Assert.Equal(pagination.Skip, deserialisedPagination.Skip);
            Assert.Equal(pagination.Limit, deserialisedPagination.Limit);
            Assert.Equal(pagination.Count, deserialisedPagination.Count);
            Assert.Equal(pagination.TotalCount, deserialisedPagination.TotalCount);
            Assert.Equal(pagination.NextPageUrl, deserialisedPagination.NextPageUrl);

        }



        [Fact]
        public void SerialisePagination_AndThen_DeserialiseToCheck_ValuesMatch_UsingBsonSerialisation()
        {
            var pagination = new Pagination(2, 500, 10, 65, "https://nextpageUrl");
            var data = pagination.ToBson();

            //convert back
            var deserialisedPagination = data.FromBson<Pagination>();

            Assert.Equal(pagination.Skip, deserialisedPagination.Skip);
            Assert.Equal(pagination.Limit, deserialisedPagination.Limit);
            Assert.Equal(pagination.Count, deserialisedPagination.Count);
            Assert.Equal(pagination.TotalCount, deserialisedPagination.TotalCount);
            Assert.Equal(pagination.NextPageUrl, deserialisedPagination.NextPageUrl);

        }

    }
}
