using KenticoCloud.Delivery.QueryParameters.Utilities;
using System.Collections.Generic;
using Xunit;

namespace KenticoCloud.Delivery.Tests.QueryParameters
{
    public class ContentTypeExtractorTests
    {
        private const string CONTENT_TYPE_CODENAME = "SomeContentType";

        private ContentTypeExtractor _extractor;

        public ContentTypeExtractorTests()
        {
            _extractor = new ContentTypeExtractor();
        }

        private class TypeWithContentTypeCodename
        {
            public const string Codename = CONTENT_TYPE_CODENAME;
        }

        [Fact]
        public void TryGetContentTypeCodename_WhenGivenTypeWithCodename_ReturnsTrueAndCodename()
        {
            var success = _extractor.TryGetContentTypeCodename(typeof(TypeWithContentTypeCodename), out string resultCodename);

            Assert.True(success);
            Assert.Equal("SomeContentType", resultCodename);
        }

        private class TypeWithoutContentTypeCodename
        {
            public const int Answer = 42;
        }

        [Fact]
        public void TryGetContentTypeCodename_WhenGivenTypeWithoutCodename_ReturnsFalse()
        {
            var success = _extractor.TryGetContentTypeCodename(typeof(TypeWithoutContentTypeCodename), out string resultCodename);

            Assert.False(success);
        }

        private class TypeWithContentTypeCodenameAsProperty
        {
            public string Codename
            {
                get
                {
                    return "SomeContentType";
                }
            }
        }

        [Fact]
        public void TryGetContentTypeCodename_WhenGivenTypeWithCodenameAsProperty_ReturnsFalse()
        {

            var success = _extractor.TryGetContentTypeCodename(typeof(TypeWithContentTypeCodenameAsProperty), out string resultCodename);

            Assert.False(success);
        }

        private class TypeWithContentTypeCodenameAsVariable
        {
            public string Codename = "42";
        }

        [Fact]
        public void TryGetContentTypeCodename_WhenGivenTypeWithCodenameAsVariable_ReturnsFalse()
        {
            var success = _extractor.TryGetContentTypeCodename(typeof(TypeWithContentTypeCodenameAsVariable), out string resultCodename);

            Assert.False(success);
        }

        private class TypeWithContentTypeCodenameAsPrivateField
        {
            private const string Codename = "MyPrivateCodename";
        }

        [Fact]
        public void TryGetContentTypeCodename_WhenGivenTypeWithCodenameAsPrivateField_ReturnsFalse()
        {
            var success = _extractor.TryGetContentTypeCodename(typeof(TypeWithContentTypeCodenameAsPrivateField), out string resultCodename);

            Assert.False(success);
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithCodenameAndExistingParams_AddsCodenameToParams()
        {
            var existingParams = new List<IQueryParameter>() { new SkipParameter(15) };

            var enhancedParams = new List<IQueryParameter>(_extractor.ExtractParameters<TypeWithContentTypeCodename>(existingParams));

            Assert.Equal(2, enhancedParams.Count);
            Assert.True(enhancedParams.Find(x => x.GetQueryStringParameter() == $"system.type=SomeContentType") != null);
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithCodename_CreatesNewParams()
        {
            var enhancedParams = new List<IQueryParameter>(_extractor.ExtractParameters<TypeWithContentTypeCodename>());

            Assert.Single(enhancedParams);
            Assert.True(enhancedParams.Find(x => x.GetQueryStringParameter() == $"system.type=SomeContentType") != null);
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithoutCodenameAndParams_ReturnsParams()
        {
            var existingParams = new List<IQueryParameter>() { new SkipParameter(15) };

            var enhancedParams = new List<IQueryParameter>(_extractor.ExtractParameters<TypeWithoutContentTypeCodename>(existingParams));

            Assert.Single(enhancedParams);
            Assert.True(enhancedParams.Find(x => x.GetQueryStringParameter() == $"system.type=TypeWithoutContentTypeCodename") == null);
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithCodenameAndExistingTypeParameter_DoesNotAddCodenameToParams()
        {
            var existingParams = new List<IQueryParameter>() { new EqualsFilter("system.type", CONTENT_TYPE_CODENAME) };

            var enhancedParams = new List<IQueryParameter>(_extractor.ExtractParameters<TypeWithContentTypeCodename>(existingParams));

            Assert.Single(enhancedParams);
            Assert.True(enhancedParams.Find(x => x.GetQueryStringParameter() == $"system.type={CONTENT_TYPE_CODENAME}") != null);
        }
    }
}
