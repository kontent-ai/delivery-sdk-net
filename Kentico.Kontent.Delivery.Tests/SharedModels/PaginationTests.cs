using Kentico.Kontent.Delivery.Caching.Extensions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;
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

            var deserializedPagination = JsonConvert.DeserializeObject<Pagination>(json);

            Assert.Equal(pagination.Skip, deserializedPagination.Skip);
            Assert.Equal(pagination.Limit, deserializedPagination.Limit);
            Assert.Equal(pagination.Count, deserializedPagination.Count);
            Assert.Equal(pagination.TotalCount, deserializedPagination.TotalCount);
            Assert.Equal(pagination.NextPageUrl, deserializedPagination.NextPageUrl);
        }

        [Fact]
        public void SerialisePagination_AndThen_DeserialiseToCheck_ValuesMatch_UsingBsonSerialisation()
        {
            var pagination = new Pagination(2, 500, 10, 65, "https://nextpageUrl");
            var data = pagination.ToBson();

            //convert back
            var deserializedPagination = data.FromBson<Pagination>();

            Assert.Equal(pagination.Skip, deserializedPagination.Skip);
            Assert.Equal(pagination.Limit, deserializedPagination.Limit);
            Assert.Equal(pagination.Count, deserializedPagination.Count);
            Assert.Equal(pagination.TotalCount, deserializedPagination.TotalCount);
            Assert.Equal(pagination.NextPageUrl, deserializedPagination.NextPageUrl);
        }
    }
}
