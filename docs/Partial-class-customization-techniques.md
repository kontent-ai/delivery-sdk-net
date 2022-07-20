It's recommended never to modify the generated models (`Model.Generated.cs`). Rather, it's recommended to adjust their partial counterparts (`Model.cs`). There are several techniques that we can utilize to achieve desired data structures.

## Collections (linked items) with specific types
By default, although they are strongly-typed inside, all linked item elements are represented as `IEnumerable<object>`. This is due to the fact that they can contain items of any type (element constraints are [ignored](https://github.com/Kentico/kontent-generators-net/issues/90) during code generation). If we want to access a strongly-typed collection, we have to adjust the partial class.

Suppose we have a model like this:

**Article.Generated.cs**
```csharp
public partial class Article
{
    public IEnumerable<object> RelatedArticles { get; set; }
}
```
   
It's customized counterpart should look like this:

**Article.cs**
```csharp
public partial class Article
{
    public IEnumerable<Article> ArticlesTyped => RelatedArticles.Cast<Article>();
    // .Cast<Article>(); // If the collection contains only Articles
    // .OfType<Article>(); // If the collection contains multiple types
}    
```

## Achieving abstraction
Let's say we have multiple types of products, each represented by a different model and we want to treat their shared properties as they were a single type.

**Coffee.Generated.cs**
```csharp
    public partial class Coffee
    {
        public string ProductName { get; set; }
    }
```

**Brewer.Generated.cs**
```csharp
    public partial class Brewer
    {
        public string ProductName { get; set; }
    }
```

### Via interfaces
If the said properties share their names, the easiest thing we can do is add an interface to their partials.

**IProduct**
```csharp
    public interface IProduct
    {
        string ProductName { get; set; }
    }
```

**Coffee.cs**
```csharp
    public partial class Coffee : IProduct
    {
    }
```

**Brewer.cs**
```csharp
    public partial class Brewer : IProduct
    {
    }
```

**Program.cs**
```csharp
var itemsOfVariousTypes = await client.GetItemsAsync<object>(new InFilter("system.type", "brewer", "coffee"));
foreach (IProduct p in itemsOfVariousTypes.Items)
{
    var name = p.ProductName;
    if (product is Coffee)
    {
        //...
    }
    //...
}
```

### Via a base class
If the said properties don't share the name, e.g.:

**Brewer.Generated.cs**
```csharp
    public partial class Brewer
    {
        public string ProdTitle { get; set; }
    }
```

We can create a base class:

**Product.cs**
```csharp
    public class Product
    {
        public virtual string RealProductName { get; set; }
    }
```

And extend the base classes with it

**Brewer.cs**
```csharp
    public partial class Brewer : Product
    {
        public override string RealProductName
        {
            get { return ProdTitle; }
            set { ProdTitle = value; }
        }
    }
```

**Coffee.cs**
```csharp
    public partial class Coffee : Product
    {
        public override string RealProductName
        {
            get { return ProductName; }
            set { ProductName = value; }
        }
    }
```

**Program.cs**
```csharp
var itemsOfVariousTypes = await client.GetItemsAsync<object>(new InFilter("system.type", "brewer", "coffee"));
foreach (Product p in itemsOfVariousTypes.Items)
{
    var name = p.RealProductName;
    if (product is Coffee)
    {
        //...
    }
    //...
}
```

### Via deserialization to an ancestor object
Although this doesn't directly relate to the partial class customization, to make the list complete, let's take a look at the third option which is to deserialize common properties to a different type altogether.

**Product.cs**
```csharp
    public class Product
    {        
        [JsonProperty("product_name")]
        public virtual string MyProductName { get; set; } 
        // Note that the property name doesn't have to match the element name. This enables deserialization of an element multiple times into different properties.
    }
```

**Program.cs**
```csharp
var itemsOfVariousTypes = await client.GetItemsAsync<Product>(new InFilter("system.type", "brewer", "coffee"));
foreach (Product p in itemsOfVariousTypes.Items)
{
    var name = p.MyProductName;
    //...
}
```

## Data annotations
If you want to enrich the generated properties with `Attribute`s (e.g. for [validation purposes](https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/validation)), the best way to do it is to use `Microsoft.AspNetCore.Mvc.ModelMetadataTypeAttribute`.

Say we have model like this:

**Cafe.Generated.cs**
```csharp
public partial class Cafe
{
    public string Phone { get; set; }
    public string City { get; set; }
    public string Email { get; set; }
    public string Country { get; set; }
    // ...
}
```


**Cafe.cs**
```csharp
    [ModelMetadataType(typeof(ICafeMetadata))]
    public partial class Cafe : ICafeMetadata
    {
    }
```

**ICafeMetadata.cs**
```csharp
    public interface ICafeMetadata
    {
        [DataType(DataType.PhoneNumber)]
        string Phone { get; set; }

        [EmailAddress]
        string Email { get; set; }
    }
```