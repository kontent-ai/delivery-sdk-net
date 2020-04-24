using System;
using System.Collections.Generic;
using Xunit;

namespace Kentico.Kontent.ImageTransformation.Tests
{
    public class ImageUrlBuilderTests
    {
        private const string BaseUrl = "https://test.com/assets/image.jpg";

        [Fact]
        public void ConstructorWithString_NewBuilder_BaseUrl()
        {
            var expected = new Uri(BaseUrl);

            var builder = new ImageUrlBuilder(BaseUrl);

            Assert.Equal(expected, builder.Url);
        }

        [Fact]
        public void ConstructorWithUri_NewBuilder_BaseUrl()
        {
            var expected = new Uri(BaseUrl);

            var builder = new ImageUrlBuilder(expected);

            Assert.Equal(expected, builder.Url);
        }

        [Theory]
        [InlineData(0.5, "?w=0.5")]
        [InlineData(1, "?w=1")]
        [InlineData(97.55, "?w=97.55")]
        [InlineData(97.01234567891, "?w=97.0123456789")]
        public void WithWidth_TransformedQuery(double width, string expectedQuery)
        {
            var builder = new ImageUrlBuilder(BaseUrl);

            builder.WithWidth(width);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }

        [Theory]
        [InlineData(0.5, "?h=0.5")]
        [InlineData(1, "?h=1")]
        [InlineData(97.55, "?h=97.55")]
        [InlineData(97.01234567891, "?h=97.0123456789")]
        [InlineData(200, "?h=200")]
        public void WithHeight_TransformedQuery(double height, string expectedQuery)
        {
            var builder = new ImageUrlBuilder(BaseUrl);

            builder.WithHeight(height);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }

        [Theory]
        [InlineData(50, 70, "?w=50&h=70")]
        public void WithWidthAndHeight_TransformedQuery(double width, double height, string expectedQuery)
        {
            var builder = new ImageUrlBuilder(BaseUrl);

            builder.WithWidth(width).WithHeight(height);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }

        [Theory]
        [InlineData("clip", "?fit=clip")]
        [InlineData("scale", "?fit=scale")]
        [InlineData("crop", "?fit=crop")]
        [InlineData("Crop", "?fit=crop")]
        public void WithFitMode_TransformedQuery(string fitMode, string expectedQuery)
        {
            var builder = new ImageUrlBuilder(BaseUrl);

            builder.WithFitMode(fitMode);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }

        public static IEnumerable<object[]> FitModesData()
        {
            yield return new object[] { ImageFitMode.Clip, "?fit=clip" };
            yield return new object[] { ImageFitMode.Crop, "?fit=crop" };
            yield return new object[] { ImageFitMode.Scale, "?fit=scale" };
        }

        [Theory]
        [MemberData(nameof(FitModesData))]
        public void WithFitModeEnum_TransformedQuery(ImageFitMode fitMode, string expectedQuery)
        {
            var builder = new ImageUrlBuilder(BaseUrl);

            builder.WithFitMode(fitMode);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }

        [Theory]
        [InlineData(1, "?dpr=1")]
        [InlineData(3.55, "?dpr=3.55")]
        public void WithDpr_TransformedQuery(double dpr, string expectedQuery)
        {
            var builder = new ImageUrlBuilder(BaseUrl);

            builder.WithDpr(dpr);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }

        [Theory]
        [InlineData(0.5, 0.5, 50, 50, "?rect=0.5,0.5,50,50")]
        [InlineData(500, 0.4, .3, 80, "?rect=500,0.4,0.3,80")]
        public void WithRectangleCrop_TransformedQuery(double x, double y, double width, double height,
            string expectedQuery)
        {
            var builder = new ImageUrlBuilder(BaseUrl);

            builder.WithRectangleCrop(x, y, width, height);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }

        [Theory]
        [InlineData(0.5, 0.5, 2, "?fit=crop&crop=focalpoint&fp-x=0.5&fp-y=0.5&fp-z=2")]
        [InlineData(50, 35.764, 5, "?fit=crop&crop=focalpoint&fp-x=50&fp-y=35.764&fp-z=5")]
        [InlineData(550, 1100.0, 3, "?fit=crop&crop=focalpoint&fp-x=550&fp-y=1100&fp-z=3")]
        public void WithFocalPointCrop_TransformedQuery(double x, double y, double z, string expectedQuery)
        {
            var builder = new ImageUrlBuilder(BaseUrl);

            builder.WithFocalPointCrop(x, y, z);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }

        [Theory]
        [InlineData("gif", "?fm=gif")]
        [InlineData("png", "?fm=png")]
        [InlineData("png8", "?fm=png8")]
        [InlineData("jpg", "?fm=jpg")]
        [InlineData("pjpg", "?fm=pjpg")]
        [InlineData("webp", "?fm=webp")]
        [InlineData("Jpg", "?fm=jpg")]
        [InlineData("JPG", "?fm=jpg")]
        public void WithFormat_TransformedQuery(string format, string expectedQuery)
        {
            var builder = new ImageUrlBuilder(BaseUrl);

            builder.WithFormat(format);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }

        public static IEnumerable<object[]> FormatData()
        {
            yield return new object[] { ImageFormat.Gif, "gif" };
            yield return new object[] { ImageFormat.Jpg, "jpg" };
            yield return new object[] { ImageFormat.Pjpg, "pjpg" };
            yield return new object[] { ImageFormat.Png, "png" };
            yield return new object[] { ImageFormat.Png8, "png8" };
            yield return new object[] { ImageFormat.Webp, "webp" };
        }

        [Theory]
        [MemberData(nameof(FormatData))]
        public void WithFormatEnum_TransformedQuery(ImageFormat format, string expectedFormat)
        {
            var builder = new ImageUrlBuilder(BaseUrl);
            var expectedQuery = $"?fm={expectedFormat}";

            builder.WithFormat(format);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }

        [Theory]
        [InlineData(20, "?q=20")]
        [InlineData(60.66, "?q=60.66")]
        public void WithQuality_TransformedQuery(double quality, string expectedQuery)
        {
            var builder = new ImageUrlBuilder(BaseUrl);

            builder.WithQuality(quality);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }

        [Theory]
        [InlineData(ImageCompression.Lossless, "?lossless=true")]
        [InlineData(ImageCompression.Lossy, "?lossless=false")]
        public void WithCompression_TransformedQuery(ImageCompression compression, string expectedQuery)
        {
            var builder = new ImageUrlBuilder(BaseUrl);

            builder.WithCompression(compression);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }

        [Fact]
        public void WithAutomaticFormat_TransformedQuery()
        {
            var builder = new ImageUrlBuilder(BaseUrl);

            builder.WithAutomaticFormat();

            Assert.Equal("?auto=webp", builder.Url.Query);
        }

        [Theory]
        [MemberData(nameof(FormatData))]
        public void WithAutomaticCompressionAndBackup_TransformedQuery(ImageFormat format, string expectedFormat)
        {
            var builder = new ImageUrlBuilder(BaseUrl);
            var expectedQuery = $"?fm={expectedFormat}&auto=webp";

            builder.WithAutomaticFormat(format);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }

        [Fact]
        public void SetTransformationMultipleTimes_LastOverwrites()
        {
            var builder = new ImageUrlBuilder(BaseUrl);
            var expectedQuery = "?w=100&h=100";

            builder.WithWidth(15)
                .WithWidth(50)
                .WithHeight(30)
                .WithHeight(100)
                .WithWidth(100);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }

        [Fact]
        public void ComplexTransformation_TransformedQuery()
        {
            var builder = new ImageUrlBuilder(BaseUrl);
            var expectedQuery = "?w=100&h=100&fit=scale&dpr=2&rect=0.5,0.5,20,20&fm=jpg&auto=webp&lossless=true";

            builder.WithWidth(100)
                .WithHeight(100)
                .WithFitMode(ImageFitMode.Scale)
                .WithDpr(2)
                .WithRectangleCrop(0.5, 0.5, 20, 20)
                .WithAutomaticFormat(ImageFormat.Jpg)
                .WithCompression(ImageCompression.Lossless);

            Assert.Equal(expectedQuery, builder.Url.Query);
        }
    }
}