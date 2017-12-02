using KenticoCloud.Delivery.QueryParameters.Utilities;
using System.Collections.Generic;
using Xunit;

namespace KenticoCloud.Delivery.Tests.QueryParameters
{
    public class ContentTypeExtractorTests
    {
        private ContentTypeExtractor _extractor;

        public ContentTypeExtractorTests()
        {
            _extractor = new ContentTypeExtractor();
        }

        private class TypeWithContentTypeCodename
        {
            public const string Codename = "SomeContentType";
        }

        [Fact]
        public void TryGetContentTypeCodename_WhenGivenTypeWithCodename_ReturnsTrueAndCodename()
        {
            string resultCodename;

            var success = _extractor.TryGetContentTypeCodename(typeof(TypeWithContentTypeCodename), out resultCodename);

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
            string resultCodename;

            var success = _extractor.TryGetContentTypeCodename(typeof(TypeWithoutContentTypeCodename), out resultCodename);

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
            string resultCodename;

            var success = _extractor.TryGetContentTypeCodename(typeof(TypeWithContentTypeCodenameAsProperty), out resultCodename);

            Assert.False(success);
        }

        private class TypeWithContentTypeCodenameAsVariable
        {
            public string Codename;
        }

        [Fact]
        public void TryGetContentTypeCodename_WhenGivenTypeWithCodenameAsVariable_ReturnsFalse()
        {
            string resultCodename;

            var success = _extractor.TryGetContentTypeCodename(typeof(TypeWithContentTypeCodenameAsVariable), out resultCodename);

            Assert.False(success);
        }

        private class TypeWithContentTypeCodenameAsPrivateField
        {
            private const string Codename = "MyPrivateCodename";
        }

        [Fact]
        public void TryGetContentTypeCodename_WhenGivenTypeWithCodenameAsPrivateField_ReturnsFalse()
        {
            string resultCodename;

            var success = _extractor.TryGetContentTypeCodename(typeof(TypeWithContentTypeCodenameAsPrivateField), out resultCodename);

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

            Assert.Equal(1, enhancedParams.Count);
            Assert.True(enhancedParams.Find(x => x.GetQueryStringParameter() == $"system.type=SomeContentType") != null);
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithoutCodenameAndParams_ReturnsParams()
        {
            var existingParams = new List<IQueryParameter>() { new SkipParameter(15) };

            var enhancedParams = new List<IQueryParameter>(_extractor.ExtractParameters<TypeWithoutContentTypeCodename>(existingParams));

            Assert.Equal(1, enhancedParams.Count);
            Assert.True(enhancedParams.Find(x => x.GetQueryStringParameter() == $"system.type=TypeWithoutContentTypeCodename") == null);
        }
    }
}
