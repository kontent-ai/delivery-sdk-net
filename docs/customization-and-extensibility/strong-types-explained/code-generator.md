## The Code Generator
You don't have to generate your strong types manually. Instead, you can use a simple yet powerful command-line [generator](https://github.com/kontent-ai/model-generator-net). The tool is available as a .NET Tool and can be used either [manually](https://github.com/kontent-ai/boilerplate-net/blob/7267e647ab84df56e174a1ba76a65a948050cf34/src/content/Kontent.Ai.Boilerplate/Tools/GenerateModels.ps1#L3), as a [pre-build event](https://github.com/kontent-ai/boilerplate-net/blob/7267e647ab84df56e174a1ba76a65a948050cf34/src/content/Kontent.Ai.Boilerplate/Kontent.Ai.Boilerplate.csproj#L37-L50), or as part of your complex continuous integration process.

**Installation:**

`dotnet tool install -g Kontent.Ai.ModelGenerator`

**Usage:**

`dotnet tool run KontentModelGenerator --projectid "<projectid>" [--namespace "<custom-namespace>"] [--outputdir "<output-directory>"] [--withtypeprovider <True|False>] [--structuredmodel <True|False>] [--filenamesuffix "<suffix>"]`

The following is a real-world example of our [MVC sample app](https://github.com/kontent-ai/sample-app-net/) …

`KontentModelGenerator.exe --projectid "975bf280-fd91-488c-994c-2f04416e5ee3" --namespace "DancingGoat.Models" --outputdir "C:\Users\jan.lenoch\Source\Repos\kontent-sample-app-net\DancingGoat\Models\ContentTypes" --withtypeprovider true`

This is what it produces:

![](https://us.v-cdn.net/6029479/uploads/editor/om/9m0zsikrmtaw.png "")

![](https://us.v-cdn.net/6029479/uploads/editor/ow/apet6zu6a451.png "")

The utility connects to the public API endpoint for content types, fetches the names, code names, content elements, their types etc. It's easy; you don't even have to provide any credentials since the endpoint is public.

### What Types Does It Produce

The following table shows all Kontent.ai element types with their corresponding .NET types.

| **Content Type** | **.NET Type** |
|-----------------|-----------------------------------|
| Text | `string` |
| Rich text | `string` |
| Number | `decimal?` |
| Multiple choice | `IEnumerable` of `IMultipleChoiceOption` |
| Date & time | `DateTime?` |
| Asset | `IEnumerable` of `IAsset` |
| Modular content | `IEnumerable` of `object` |
| Taxonomy | `IEnumerable` of `ITaxonomyTerm` |
| URL slug  | `string` |

All generated classes are partial. If you extend them in separate files, you can re-generate the original ones without worrying about losing your code.

### The Type Provider

The `--withtypeprovider true` option makes the generator also produce a simple class that maps code names of content types to the generated .NET types. Like so:
```csharp
public class CustomTypeProvider : ITypeProvider
{
	protected static readonly Dictionary<Type, string> Codenames = new Dictionary<Type, string>
	{
		{typeof(Article), "article"},
		{typeof(Brewer), "brewer"},
		...
	};

	public virtual Type GetType(string contentType)
	{
		return Codenames.Keys.FirstOrDefault(type => GetCodename(type).Equals(contentType));
	}

	public virtual string GetCodename(Type contentType)
	{
		return Codenames.TryGetValue(contentType, out var codename) ? codename : null;
	}
}
```

The important point is that you can override this simple logic with your own one.

As an example, you can create several Kontent.ai content types that don't necessarily need separate .NET types. You can name these content types with some prefix (like 'Aggregated') and return the common .NET type for all of them:

```csharp
	if (contentType.StartsWith("Aggregated"))
	{
		return typeof(SomeAggregateType)
	}
```

You can mix and match to your liking.
