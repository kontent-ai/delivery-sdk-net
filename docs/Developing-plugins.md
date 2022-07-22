Are you developing a plugin or tool based on this SDK? Great! Then please include the [source tracking header](https://github.com/kontent-ai/Home/wiki/Guidelines-for-Kontent-related-tools#analytics) to your code. This way, we'll be able to identify that the traffic to Kontent.ai APIs is originating from your plugin and will be able to share its statistics with you!

You can either attach it to the **AssemblyInfo.cs**
```c#
[assembly: DeliverySourceTrackingHeader()]
```

Or to the **.csproj**:

```xml
  <ItemGroup>
    <AssemblyAttribute Include="Kontent.Ai.Delivery.DeliverySourceTrackingHeader" />
  </ItemGroup>
```

By default, it'll load the necessary info (package name + version) from your assembly. If you want to customize it, please use one of the other constructors:

```c#
// You specify the name, the version is extracted from the assembly
public DeliverySourceTrackingHeaderAttribute(string packageName)

// Or you specify the name and the version
public DeliverySourceTrackingHeaderAttribute(string packageName, int majorVersion, int minorVersion, int patchVersion, string preReleaseLabel = null)
```

If you use the **.csproj***:
```xml
<AssemblyAttribute>
    <_Parameter1>MyPackage</_Parameter1>
</AssemblyAttribute>
```