using System;
using Kontent.Ai.Delivery.Caching.Extensions;
using Kontent.Ai.Delivery.SharedModels;
using Newtonsoft.Json;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.SharedModels
{
    public class PaginationTests
    {
        [Fact]
        public void SerializePagination_AndThen_DeserializeToCheck_ValuesMatch_UsingStandardSerialization()
        {
            ExecuteTestsScenario(SerializeThenDeserializeUsingJson);
        }
        [Fact]
        public void SerializePagination_AndThen_DeserializeToCheck_ValuesMatch_UsingBsonSerialization()
        {
            ExecuteTestsScenario(SerializeThenDeserializeUsingBson);
        }
        private void ExecuteTestsScenario(Func<Pagination, Pagination> serializeThenDeserialize)
        {
            var pagination = new Pagination(2, 500, 10, 65, "https://nextpageUrl");
            var deserializedPagination = serializeThenDeserialize(pagination);
            Assert.Equal(pagination.Skip, deserializedPagination.Skip);
            Assert.Equal(pagination.Limit, deserializedPagination.Limit);
            Assert.Equal(pagination.Count, deserializedPagination.Count);
            Assert.Equal(pagination.TotalCount, deserializedPagination.TotalCount);
            Assert.Equal(pagination.NextPageUrl, deserializedPagination.NextPageUrl);
        }

        private Pagination SerializeThenDeserializeUsingJson(Pagination pagination)
        {
            var json = JsonConvert.SerializeObject(pagination);
            return JsonConvert.DeserializeObject<Pagination>(json);
        }

        private Pagination SerializeThenDeserializeUsingBson(Pagination pagination)
        {
            var data = pagination.ToBson();
            return data.FromBson<Pagination>();
        }
    }
}
