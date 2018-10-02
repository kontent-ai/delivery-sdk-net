using System;
using System.Collections.Generic;
using FakeItEasy;
using Microsoft.Extensions.Options;
using Xunit;

namespace KenticoCloud.Delivery.Tests.QueryParameters
{
    public class ContentTypeExtractorTests
    {
        private const string CONTENT_TYPE_CODENAME = "SomeContentType";
        private const string FAKE_PROJECT_ID = "00000000-0000-0000-0000-000000000000";

        private readonly DeliveryClient _client;
        private readonly ICodeFirstTypeProvider _contentTypeProvider;

        public ContentTypeExtractorTests()
        {
            _contentTypeProvider = A.Fake<ICodeFirstTypeProvider>();

            A.CallTo(() => _contentTypeProvider.GetCodename(typeof(TypeWithContentTypeCodename))).Returns(TypeWithContentTypeCodename.Codename);
            A.CallTo(() => _contentTypeProvider.GetCodename(typeof(TypeWithoutContentTypeCodename))).Returns(null);

            _client = new DeliveryClient(
                new OptionsWrapper<DeliveryOptions>(new DeliveryOptions{ProjectId = FAKE_PROJECT_ID}),
                null,
                null, 
                new CodeFirstModelProvider(null, null, _contentTypeProvider, null)
           );
        }

        private class TypeWithContentTypeCodename
        {
            public const string Codename = CONTENT_TYPE_CODENAME;
        }

        private class TypeWithoutContentTypeCodename
        {
            public const int Answer = 42;
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithCodenameAndExistingParams_AddsCodenameToParams()
        {
            var existingParams = new List<IQueryParameter>() { new SkipParameter(15) };

            var enhancedParams = new List<IQueryParameter>(_client.ExtractParameters<TypeWithContentTypeCodename>(existingParams));

            Assert.Equal(2, enhancedParams.Count);
            Assert.True(enhancedParams.Find(x => x.GetQueryStringParameter() == $"system.type={CONTENT_TYPE_CODENAME}") != null);
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithCodename_CreatesNewParams()
        {
            var enhancedParams = new List<IQueryParameter>(_client.ExtractParameters<TypeWithContentTypeCodename>());

            Assert.Single(enhancedParams);
            Assert.True(enhancedParams.Find(x => x.GetQueryStringParameter() == $"system.type={CONTENT_TYPE_CODENAME}") != null);
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithoutCodenameNoParams_CreatesEmptyParams()
        {
            var enhancedParams = new List<IQueryParameter>(_client.ExtractParameters<TypeWithoutContentTypeCodename>());

            Assert.Empty(enhancedParams);
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithoutCodenameAndParams_ReturnsParams()
        {
            var existingParams = new List<IQueryParameter>() { new SkipParameter(15) };

            var enhancedParams = new List<IQueryParameter>(_client.ExtractParameters<TypeWithoutContentTypeCodename>(existingParams));

            Assert.Single(enhancedParams);
            Assert.True(enhancedParams.Find(x => x.GetQueryStringParameter() == $"system.type=TypeWithoutContentTypeCodename") == null);
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithCodenameAndExistingTypeParameter_DoesNotAddCodenameToParams()
        {
            var existingParams = new List<IQueryParameter>() { new EqualsFilter("system.type", CONTENT_TYPE_CODENAME) };

            var enhancedParams = new List<IQueryParameter>(_client.ExtractParameters<TypeWithContentTypeCodename>(existingParams));

            Assert.Single(enhancedParams);
            Assert.True(enhancedParams.Find(x => x.GetQueryStringParameter() == $"system.type={CONTENT_TYPE_CODENAME}") != null);
        }
    }
}
