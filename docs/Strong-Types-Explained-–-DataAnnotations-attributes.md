This article explains how to add [DataAnnotations attributes for validation purposes](https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/validation) to the generated models.

Once you've [generated your strongly-typed models](https://github.com/kontent-ai/model-generator-net), you can call the generic overload of the `GetItemAsync<T>` method:

```csharp
	var response = await deliveryClient.GetItemAsync<Article>("on_roasts");
```

This will return an `Article` object with the following members:

* `IEnumerable<ITaxonomyTerm> Personas`
* `string Title`
* `IEnumerable<IAsset> TeaserImage`
* `DateTime? PostDate`
* `string Summary`
* `string BodyCopy`
* `IEnumerable<object> RelatedArticles`
* `string MetaKeywords`
* `string MetaDescription`
* `string UrlPattern`
* `IContentItemSystemAttributes System`

Even the `RelatedArticles` property, in fact, points to an `IEnumerable<Article>` collection, in this case. The items in that collection are represented by [heap](https://www.codeproject.com/Articles/76153/Six-important-NET-concepts-Stack-heap-value-types) objects with proper types. We'll get to that point in [part 5](https://github.com/kontent-ai/delivery-sdk-net/docs/Strong-Types-Explained---How-to-Use-Runtime-Typing.md) of this series.

### The DataAnnotations Attributes

Sometimes, not all data can be distinguished with .NET types. For example, in Kontent.ai, an e-mail address is nothing but a string:

`john.doe@example.com`

The same applies to phone numbers if they contain the plus sign in the beginning and dashes in the middle:

`+1-877-692-4978`

How can you make sure that the two properties of the same type [string](https://docs.microsoft.com/en-us/dotnet/api/system.string?view=netstandard-1.6) have different semantics? By using [System.ComponentModel.DataAnnotations](https://docs.microsoft.com/de-de/dotnet/core/api/system.componentmodel.dataannotations) attributes.

Let's take a few [examples](https://github.com/kontent-ai/sample-app-net/tree/master/DancingGoat/Models) in our MVC sample site:


```csharp
	[DataType(DataType.PhoneNumber)]
	public string Phone { get; set; }

	[EmailAddress]
	public string Email { get; set; }

	[DataType(DataType.Html)]
	public string LongDescription { get; set; }
```

By decorating the properties in the model class with a few attributes you give a signal about the fine-grained type. The framework, MVC, WinForms, and various applications have been taking these attributes into account over time. Using these attributes became a convention among parts of the .NET world.

But how do you decorate the properties of a generated class? If you re-generate classes, the attributes will get lost. The obvious solution - creating 'mirrored' properties in the same class - can't be done.

Fortunately, the [ModelMetadataTypeAttribute](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.modelmetadatatypeattribute) comes to the rescue.

Just create another simple class with properties with the same name, of the same type and visibility. Decorate these with DataAnnotations attributes and decorate the original class with the `ModelMetadataType` attribute. Like that:

```csharp
	using Microsoft.AspNetCore.Mvc;

	namespace DancingGoat.Models
	{
		[ModelMetadataType(typeof(ICoffeeMetadata))]
		public partial class Coffee
		{
		}

		public interface ICoffeeMetadata
		{
			[DataType(DataType.Html)]
			string ShortDescription { get; set; }

			[DataType(DataType.Html)]
			string LongDescription { get; set; }
		}
	}
```

