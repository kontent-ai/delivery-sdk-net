using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace KenticoKontent.Delivery.ImageTransformation
{
    /// <summary>
    /// Provides a builder for Image Transformations for Kentico Kontent Delivery API.
    /// </summary>
    public sealed class ImageUrlBuilder
    {
        private readonly Uri _assetUrl;
        private readonly Dictionary<string, StringValues> _queryParameters = new Dictionary<string, StringValues>();
        private string Query =>_queryParameters.Any() ? $"?{string.Join("&", _queryParameters.Select(x => $"{x.Key}={x.Value}"))}" : "";

        /// <summary>
        /// Gets the <see cref="T:System.Uri"/> instance with applied transformations.
        /// </summary>
        public Uri Url => new Uri(_assetUrl, Query);

        /// <summary>
        /// Initializes a new instance of the ImageUrlBuilder class with the specified URI
        /// </summary>
        /// <param name="assetUrl">An asset URI. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="assetUrl" /> is null. </exception>
        public ImageUrlBuilder(Uri assetUrl)
        {
            _assetUrl = assetUrl ?? throw new ArgumentNullException(nameof(assetUrl));
        }

        /// <summary>
        /// Initializes a new instance of the ImageUrlBuilder class with the specified string URI.
        /// </summary>
        /// <param name="assetUrl">A string representing asset URI. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="assetUrl" /> is null. </exception>
        public ImageUrlBuilder(string assetUrl) : this(new Uri(assetUrl))
        {
        }

        /// <summary>
        /// The width transformation enables dynamic width resizing based on pixels and percent values.
        /// </summary>
        /// <param name="width">A required image width.</param>
        /// <returns>The same <see cref="ImageUrlBuilder" /> instance. </returns>
        public ImageUrlBuilder WithWidth(double width)
        {
            _queryParameters["w"] = FormatDouble(width);
            return this;
        }

        /// <summary>
        /// The height transformation enables dynamic height resizing based on pixels and percent values.
        /// </summary>
        /// <param name="height">A required image height. </param>
        /// <returns>The same <see cref="ImageUrlBuilder" /> instance. </returns>
        public ImageUrlBuilder WithHeight(double height)
        {
            _queryParameters["h"] = FormatDouble(height);
            return this;
        }

        /// <summary>
        /// The fit transformation controls how the output image is fit to its target dimensions after resizing.
        /// </summary>
        /// <param name="fitMode">Specifies the mode for the transformation. </param>
        /// <returns>The same <see cref="ImageUrlBuilder" /> instance. </returns>
        public ImageUrlBuilder WithFitMode(string fitMode)
        {
            if (!string.IsNullOrWhiteSpace(fitMode) && Enum.TryParse(fitMode, ignoreCase: true, result: out ImageFitMode parsedFitMode))
            {
                WithFitMode(parsedFitMode);
            }
            return this;
        }

        /// <summary>
        /// The fit transformation controls how the output image is fit to its target dimensions after resizing.
        /// </summary>
        /// <param name="fitMode">Specifies the mode for the transformation. </param>
        /// <returns>The same <see cref="ImageUrlBuilder" /> instance. </returns>
        public ImageUrlBuilder WithFitMode(ImageFitMode fitMode)
        {
            _queryParameters["fit"] = StringifyEnum(fitMode);
            return this;
        }

        /// <summary>
        /// The dpr transformation is used to serve correctly sized images for devices that expose a device pixel ratio.
        /// </summary>
        /// <param name="dpr">A required DPR value. </param>
        /// <returns>The same <see cref="ImageUrlBuilder" /> instance. </returns>
        public ImageUrlBuilder WithDpr(double dpr)
        {
            _queryParameters["dpr"] = FormatDouble(dpr);
            return this;
        }

        /// <summary>
        /// Applies the crop transformation that removes pixels from an image outside the specified rectangle.
        /// </summary>
        /// <param name="x">Rectangle offset on the X-axis. </param>
        /// <param name="y">Rectangle offset on the Y-axis.</param>
        /// <param name="width">Rectangle width. </param>
        /// <param name="height">Rectangle height. </param>
        /// <returns>The same <see cref="ImageUrlBuilder" /> instance. </returns>
        public ImageUrlBuilder WithRectangleCrop(double x, double y, double width, double height)
        {
            _queryParameters["rect"] = string.Join(",", new [] {x, y, width, height}.Select(FormatDouble));
            return this;
        }

        /// <summary>
        /// Applies the crop transformation centered on the specified point.
        /// </summary>
        /// <param name="x">Focal point X coordinate. </param>
        /// <param name="y">Focal point Y coordinate. </param>
        /// <param name="z">Zoom of the transformation. </param>
        /// <returns>The same <see cref="ImageUrlBuilder" /> instance. </returns>
        public ImageUrlBuilder WithFocalPointCrop(double x, double y, double z)
        {
            WithFitMode(ImageFitMode.Crop);
            _queryParameters["crop"] = "focalpoint";
            _queryParameters["fp-x"] = FormatDouble(x);
            _queryParameters["fp-y"] = FormatDouble(y);
            _queryParameters["fp-z"] = FormatDouble(z);
            return this;
        }

        /// <summary>
        /// The format transformation enables the source image to be converted (a.k.a., "transcoded") from one encoded format to another. This is very useful when the source image has been saved in a sub-optimal file format that hinders performance.
        /// </summary>
        /// <param name="format">The output image format. </param>
        /// <returns>The same <see cref="ImageUrlBuilder" /> instance. </returns>
        public ImageUrlBuilder WithFormat(string format)
        {
            if (!string.IsNullOrWhiteSpace(format) && Enum.TryParse(format, ignoreCase: true, result: out ImageFormat parsedFormat))
            {
                WithFormat(parsedFormat);
            }
            return this;
        }

        /// <summary>
        /// The format transformation enables the source image to be converted (a.k.a., "transcoded") from one encoded format to another. This is very useful when the source image has been saved in a sub-optimal file format that hinders performance.
        /// </summary>
        /// <param name="format">Target image file type. </param>
        /// <returns>The same <see cref="ImageUrlBuilder" /> instance. </returns>
        public ImageUrlBuilder WithFormat(ImageFormat format)
        {
            _queryParameters["fm"] = StringifyEnum(format);
            return this;
        }

        /// <summary>
        /// Applies the quality parameter that enables control over the compression level for lossy file-formatted images.
        /// </summary>
        /// <param name="quality">The required quality of the image. </param>
        /// <returns>The same <see cref="ImageUrlBuilder" /> instance. </returns>
        public ImageUrlBuilder WithQuality(double quality)
        {
            _queryParameters["q"] = FormatDouble(quality);
            return this;
        }

        /// <summary>
        /// Specifies the compression mode for the WebP image transformations.
        /// </summary>
        /// <param name="compression">Specifies the lossy or lossless compression. </param>
        /// <returns>The same <see cref="ImageUrlBuilder" /> instance. </returns>
        public ImageUrlBuilder WithCompression(ImageCompression compression)
        {
            _queryParameters["lossless"] = compression == ImageCompression.Lossless ? "true" : "false";
            return this;
        }

        /// <summary>
        /// Enables WebP image support.
        /// </summary>
        /// <returns>The same <see cref="ImageUrlBuilder" /> instance. </returns>
        public ImageUrlBuilder WithAutomaticFormat()
        {
            _queryParameters["auto"] = StringifyEnum(ImageFormat.Webp);
            return this;
        }

        /// <summary>
        /// Enables WebP image support with format for non supporting WebP browsers.
        /// </summary>
        /// <param name="backupFormat">Image format for non supporting browsers. </param>
        /// <returns>The same <see cref="ImageUrlBuilder" /> instance. </returns>
        public ImageUrlBuilder WithAutomaticFormat(ImageFormat backupFormat)
        {
            return WithFormat(backupFormat).WithAutomaticFormat();
        }

        private static string FormatDouble(double number) => number.ToString("0.##########", CultureInfo.InvariantCulture);

        private static string StringifyEnum(Enum value) => value.ToString().ToLowerInvariant();
    }
}