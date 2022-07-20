## Automatic Typing at Runtime

Let's take a look at what automatic runtime typing is, why we've implemented it and how you can use it.

### Why

Mainly because of MVC. Back in 2009, the ASP.NET MVC 2 brought a very robust, yet simple and flexible feature called Display templates (and Editor templates, to be precise). You can read more about it in one of the [MVC author's blogpost](http://bradwilson.typepad.com/blog/2009/10/aspnet-mvc-2-templates-part-1-introduction.html) or in a [short excerpt in a recent article](https://devnet.kentico.com/articles/render-online-forms-with-asp-net-mvc-display-templates-and-the-code-generator—part-1).

Templates allow the developer to define markup (visuals) for a .NET type once and use it throughout the whole MVC app automatically (unless other visuals were selected explicitly).

The beauty of templates really shines in a situation when the MVC app obtains a collection of items of various types from the data store. In this situation, the developer does not have to care about defining the types of items and selecting templates. As long as there is a template defined for a type in the app, MVC runtime recognizes the type and renders the proper markup automatically.

But, in order for MVC to be able to select a proper template, the collection of content items cannot be typed to just one type.

To get several content items of various types, you cannot call:

```csharp
	var itemsOfVariousTypes = deliveryClient.GetItemsAsync<Brewer>(new InFilter("system.type", "brewer", "coffee"));
```

Moreover, there is another situation where it is not desired to state the type upfront: modular content. 

You probably know that content items can be aggregated into another content item via a modular content element. In the Draft UI, users can assign multiple items of various types to that element. The items can fluctuate over time, right according to the edits that the business users do.

The situation with modular content can be demonstrated on a fictional Article type. The following is what MVC needs. In the `ModularContentField` property, the individual collection items need to be strongly typed:

```csharp
	var article = new Article
	{
		Perex = "Lorem ipsum dolor sit <em>amet</em>.",
		TeaserImageUri = "http://example.com/image.png",
		BodyCopy = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
		ModularContentField = new List<object>
		{
			new Comment { ItemGuid = Guid.NewGuid(), Person = "John Doe", Text = "I don't agree with the article!" },
			new Comment { ItemGuid = Guid.NewGuid(), Person = "Jane Doe", Text = "I fully agree with with you!" },
			new Review { ItemGuid = Guid.NewGuid(), Rating = 10, Reviewer = "Jan Lenoch", ReviewText = "Great article. Informative, easy to understand." }
		}
	}
```

The items in the `ModularContentField` collection can be of whatever type, but they need to be strongly typed at the same time.

And this is exactly what the automatic runtime typing brings to the table. With runtime typing, the app retrieves data properly typed, even though the MVC developer had not specified a type of collection items upfront.

### Why Not to Use Dynamic

The [dynamic](https://docs.microsoft.com/en-us/dotnet/articles/csharp/programming-guide/types/using-type-dynamic) type is a great addition to the otherwise static .NET ecosystem. You can declare data as dynamic without knowing which specific members of what type it will contain at runtime. When the app retrieves some specific data, the [DLR](http://msdn.microsoft.com/library/f769a271-8aff-4bea-bfab-6160217ce23d) performs a discovery of all members. The only obvious difference from using static types is that references to non-existent properties result in a runtime exception instead of compiler errors.

Originally, we've been thinking about using `dynamic`. But the problem is that it produces anonymous (unknown) types. Like so:

```csharp
	var article = new Article
	{
		Perex = "Lorem ipsum dolor sit <em>amet</em>.",
		TeaserImageUri = "http://example.com/image.png",
		BodyCopy = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
		ModularContentField = new List<object>
		{
			new { ItemGuid = Guid.NewGuid(), Person = "John Doe", Text = "I don't agree with the article!" },
			new { ItemGuid = Guid.NewGuid(), Person = "Jane Doe", Text = "I fully agree with with you!" },
			new { ItemGuid = Guid.NewGuid(), Rating = 10, Reviewer = "Jan Lenoch", ReviewText = "Great article. Informative, easy to understand." }
		}
	}
```

(The difference can be seen in the `ModularContentField` property. The collection items miss their type qualifiers.)

With anonymous or otherwise unknown types, MVC cannot determine the template automatically. That's why we've implemented the runtime typing.

## How to Use Runtime Typing

The only thing you need to do is fetch items as bare objects via the type parameter being set to `object`:

```csharp
	var itemsOfVariousTypes = deliveryClient.GetItemsAsync<object>(new InFilter("system.type", "brewer", "coffee"));
```

The response would look like the following:

```csharp
	var itemsOfVariousTypes = new IEnumerable<object>
	{
		new Brewer
		{
			ProductName = "AeroPress",
			…
			Manufacturer = "Aerobie"
		},
		new Coffee
		{
			ProductName = "Brazil Natural Barra Grande",
			…
			Farm = "Sitio Barra Grande"
		}
	}
```

The developer, or an existing framework like MVC, is then free to work with the data in whatever way they want. They have all the type information included. It is up to them to decide whether and how to use it.

In MVC, all that's required is to define display templates for all types in the respective filesystem locations (like `~/Views/Shared/DisplayTemplates/[type name].cshtml`). MVC will use the type information to pick the right template.
