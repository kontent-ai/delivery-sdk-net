using System;
using Kentico.Kontent.Delivery.Caching.Extensions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.SharedModels
{
    public class PaginationTests
    {
        [Fact]
        public void SerializePagination_AndThen_DeserializeToCheck_ValuesMatch_UsingBsonSerialization()
        {
        private void ExecuteTestsScenario(Func<Pagination,Pagination> serializeThenDeserialize)
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
