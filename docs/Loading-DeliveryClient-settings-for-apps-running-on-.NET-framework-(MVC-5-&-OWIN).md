In a legacy ASP.NET MVC 5 app, you might want to load your configuration from the *web.config* file to set up the `DeliveryClient`.

In such a case, you can take advantage of the [`ConfigurationManagerProvider`](https://github.com/Kentico/kontent-sample-app-net/blob/96139dfba7b3c6f0276420d509c3562c403ff2e7/DancingGoat/Infrastructure/ConfigurationManagerProvider.cs) which makes it easy to access the appSettings as any other configuration source.

```xml
<configuration>
  	<appSettings>    
    	<add key="ProjectId" value="<ProjectId>" />
        ...
  	</appSettings>
</configuration>
```

There are 2 approaches:

* Using the `ConfigurationManagerProvider` with the [Configuration API](Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core)

  ```csharp
  var builder = new ConfigurationBuilder()
  		.Add(new ConfigurationManagerProvider())
        .Build();
  ```

* A `GetDeliveryOptions()` method call

  ```csharp
  var configurationProvider = new ConfigurationManagerProvider();
  var client = DeliveryClientBuilder.WithOptions(builder => configurationProvider.GetDeliveryOptions()).Build();
  ```
