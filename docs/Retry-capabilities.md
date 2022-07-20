By default, the SDK uses a retry policy, asking for requested content again in case of an error. You can disable the retry policy by setting the `DeliveryOptions.EnableRetryPolicy` parameter to `false`. The default policy retries the HTTP requests if the following status codes are returned:

* 408 - `RequestTimeout` 
* 429 - `TooManyRequests`
* 500 - `InternalServerError`
* 502 - `BadGateway`
* 503 - `ServiceUnavailable`
* 504 - `GatewayTimeout`

or if there is one of the following connection problems:

* `ConnectFailure`
* `ConnectionClosed`
* `KeepAliveFailure`
* `NameResolutionFailure`
* `ReceiveFailure`
* `SendFailure`
* `Timeout`

The default retry policy performs retries using a randomized exponential backoff scheme to determine the interval between retries. It can be customized by changing parameters in `DeliveryOptions.RetryPolicyOptions`. The `DeltaBackoff` parameter specifies the back-off interval between retries. The `MaxCumulativeWaitTime` parameter specifies the maximum cumulative wait time. If the cumulative wait time exceeds this value, the client will stop retrying and return the error to the application. The default retry policy also respects the `Retry-After` response header.

```csharp
IDeliveryClient client = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithProjectId("<YOUR_PROJECT_ID>")
        .WithDefaultRetryPolicyOptions(new DefaultRetryPolicyOptions {
	    DeltaBackoff = TimeSpan.FromSeconds(1),
	    MaxCumulativeWaitTime = TimeSpan.FromSeconds(10)
	})
	.Build())
    .Build();
```

You can create your custom retry policy, for example with [Polly](https://github.com/App-vNext/Polly), by implementing `IRetryPolicy` and `IRetryPolicyProvider` interfaces. The custom retry policy provider can be registered with `DeliveryClientBuilder.WithRetryPolicyProvider` or with the `ServiceCollection`.