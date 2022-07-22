## Implementing Model Inheritance

Another interesting topic with strong types is inheritance. Currently, Kontent.ai content types don't inherit elements from other content types. In our [MVC sample site](https://github.com/kontent-ai/sample-app-net/), we have the Coffee, Brewer, Grinder, and Accessory content types. All of them represent some form of a product. They have the following properties in common:

* Product name
* Price
* Image
* Product status
* Short description
* Long description

By altering a property in one content type, the corresponding properties in other types wouldn't get affected. Hence, the generated .NET types follow this pattern.

But what if you wish to operate on those common properties with just one single piece of .NET code?

Although you can create a base class and make the generated one inherit from it, it won't work. Simply because you cannot define a common property at the base class level instead of those two or more properties in the generated classes.

To demonstrate it, let's suppose you have:

```csharp
    public partial class Coffee
    {
        public string ProductName { get; set; }
    }
```

… and …

```csharp
    public partial class Brewer
    {
        public string ProductName { get; set; }
    }
```

You can no longer declare the ProductName property in the Product base class:

```csharp
    public class Product
    {
        public string ProductName { get; set; }
    }
```

How to deal with it? There are two clean ways to choose from. Choosing one or another depends on whether you wish to also operate on type-specific properties (not just those common ones mentioned above).

### Deserialize Responses to an Ancestor Object

If you wish to operate on Brewers, Coffee and other specific products as objects of type `Product`, without the need to work with type-specific properties, this is the best way to go.

Simply define a custom type `Product` with those common properties. And decorate them with `JsonProperty` attributes with their corresponding Kontent.ai code names:

```csharp
    public class Product
    {
        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("price")]
        public decimal? Price { get; set; }

        // Other properties ...
    }
```

The default [PropertyMapper](https://github.com/kontent-ai/delivery-sdk-net/Kontent.Ai.Delivery/StrongTyping/PropertyMapper.cs) in our Delivery .NET SDK is smart enough to automatically bind your properties to appropriate data using these JsonProperty attributes.

Now you can request all those Brewers and Coffee off of the API endpoint as Product objects using the generic overload of the GetItemsAsync method:

```csharp
    List<Product> brewersAndCoffeeAsProducts = (await client.GetItemsAsync<Product>(new InFilter("system.type", "brewer", "coffee"))).Items.ToList();
```

This implementation requires that the content elements in all those content types be named the same way. Once they are, this pattern is the quickest to implement.

### Define Shadow Properties Aside From Existing Ones

If you need to be able to work with the data as both a `Brewer` and as a `Product` at the same time, choose this path.

You can declare a property with a slightly different name, of the same type and visibility. In the accessors of the new property, simply point to the original one:

```csharp
    public partial class Coffee : Product
    {
        public override string ProductProductName
        {
            get { return ProductName; }
            set { ProductName = value; }
        }

        public override decimal? ProductPrice
        {
            get { return Price; }
            set { Price = value; }
        }

        // Other properties ...
    }
```

Now, request those Brewers and Coffee using the automatic runtime typing functionality (explained in parts [4](https://github.com/kontent-ai/delivery-sdk-net/docs/Strong-Types-Explained---Why-the-Runtime-Typing.md) and [5](https://github.com/kontent-ai/delivery-sdk-net/docs/Strong-Types-Explained---How-to-Use-Runtime-Typing).md) by specifying `object` as the generic type parameter:

```csharp
    var brewersAndCoffeeAsProducts = (await client.GetItemsAsync<object>(new InFilter("system.type", "brewer", "coffee"))).Items;
```

That way you can do both: work with Coffee and Brewers in a uniform way while also being able to access the type-specific properties.

```csharp
    foreach (var product in brewersAndCoffeeAsProducts)
    {
        product.ProductProductName += " (discounted)";
        if (product is Coffee)
        {
            product.ShortDescription += " The price is now lower, the aroma stayed the same.";
        }
    }
```
