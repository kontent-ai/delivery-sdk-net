You can configure the `DeliveryClient` to retrieve either secured or unpublished content at one time.

# Secure your keys first
For security reasons, the `PreviewApiKey` and `SecureAccessApiKey` key should be stored outside of the project file structure. It's recommended to use [Secret Manager](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets) or [Azure Key Vault](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration) to store sensitive data. Avoid using `appsettings.json` for API keys.

# Secured content
To retrieve secured content, you need to create an instance of the `IDeliveryClient` with a Secure API key. Each Kontent.ai project has its own Secure API key.

```csharp
IDeliveryClient client = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithProjectId("<YOUR_PROJECT_ID>")
        .UseProductionApi("<YOUR_SECURE_API_KEY>")
        .Build())
    .Build();
```

or set it to the `IConfiguration` object using the `Configuration["DeliveryOptions:SecureAccessApiKey"]` and `Configuration["DeliveryOptions:UseSecureAccess"]`

**Configuration structure:**
```json
"DeliveryOptions": {
    "UseSecureAccess": true,
    "SecureAccessApiKey": "<YOUR_SECURE_API_KEY>"
  }
```

# Unpublished content
Similarly, to retrieve unpublished content, you need to create an instance of the `IDeliveryClient` with both Project ID and Preview API key. Each Kontent.ai project has its own Preview API key.

```csharp
IDeliveryClient client = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithProjectId("<YOUR_PROJECT_ID>")
        .UsePreviewApi("<YOUR_PREVIEW_API_KEY>")
        .Build())
    .Build();
```
or set it to the `IConfiguration` object using the `Configuration["DeliveryOptions:PreviewApiKey"]` and `Configuration["DeliveryOptions:UsePreviewApi"]`

**Configuration structure:**
```json
"DeliveryOptions": {
    "UsePreviewApi": true,
    "PreviewApiKey": "<YOUR_SECURE_API_KEY>"
  }
```

Learn more about [configuring content preview](https://docs.kontent.ai/tutorials/develop-apps/get-content/configuring-preview-for-content-items) for your app and Kontent project.