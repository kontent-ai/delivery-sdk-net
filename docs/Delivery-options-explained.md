* `ProjectId` – sets the ID of your Kontent.ai project. This parameter must always be set.
* `UsePreviewApi` – determines whether to use the Delivery Preview API (instead of the [Production API](https://docs.kontent.ai/reference/delivery-api#section/Production-vs.-Preview)). See [previewing unpublished content](#previewing-unpublished-content) to learn more.
* `PreviewApiKey` - sets the API key that is used to retrieve content with the Preview API. (Only used when `UsePreviewApi` is `true`.)
* `UseSecureAccess` – determines whether to authenticate requests to the production Delivery API with an API key. See [retrieving secured content](https://docs.kontent.ai/tutorials/develop-apps/get-content/securing-public-access?tech=dotnet#a-retrieving-secured-content) to learn more.
* `WaitForLoadingNewContent` – forces the client instance to wait while fetching updated content, useful when acting upon [webhook calls](https://docs.kontent.ai/tutorials/develop-apps/integrate/using-webhooks-for-automatic-updates).
* `EnableRetryPolicy` – determines whether HTTP requests will use [retry policy](./Retry-capabilities). By default, the retry policy is enabled.
* `DefaultRetryPolicyOptions` – sets a [custom parameters](./Retry-capabilities) for the default retry policy. By default, the SDK retries for at most 30 seconds.
* `DefaultRenditionPreset` - SDK will by default return asset URLs with additional rendition selection query parameter, that selects rendition with codename specified by this option
