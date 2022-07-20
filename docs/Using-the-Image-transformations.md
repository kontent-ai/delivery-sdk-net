The [ImageUrlBuilder class](https://github.com/Kentico/kontent-delivery-sdk-net/blob/master/Kentico.Kontent.ImageTransformation/ImageTransformation/ImageUrlBuilder.cs) exposes methods for applying image transformations on the Asset URL.

```csharp
string assetUrl = articleItem.GetAssets("teaser_image").First().Url;
ImageUrlBuilder builder = new ImageUrlBuilder(assetUrl);
string transformedAssetUrl = builder.WithFocalPointCrop(560, 515, 2)
                                    .WithDPR(3)
                                    .WithAutomaticFormat(ImageFormat.Png)
                                    .WithCompression(ImageCompression.Lossy)
                                    .WithQuality(85)
                                    .Url;
```

For list of supported transformations and more information visit the Kentico Delivery API reference at <https://docs.kontent.ai/reference/image-transformation>.