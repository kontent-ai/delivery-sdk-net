The `Kontent.Ai.Delivery*` NuGet packages are configured to provide [Source Link](https://docs.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink) debugging capabilities. The source code is downloaded directly from GitHub to Visual Studio.

### How to configure SourceLink

1. Open a solution with a project referencing the Kontent.Ai.Delivery (or Kontent.Ai.Delivery.RX) Nuget package.
2. Open Tools -> Options -> Debugging -> General.
    * Clear **Enable Just My Code**.
    * Select **Enable Source Link Support**.
    * (Optional) Clear **Require source files to exactly match the original version**.
3. Build your solution.
4. [Add a symbol server `https://symbols.nuget.org/download/symbols`](https://blog.nuget.org/20181116/Improved-debugging-experience-with-the-NuGet-org-symbol-server-and-snupkg.html)
  * ![Add a symbol server in VS](https://raw.githubusercontent.com/kontent-ai/kontent-delivery-sdk-net/master/.github/assets/vs-nuget-symbol-server.PNG)
5. Run a debugging session and try to step into the Kontent.Ai.Delivery code.
6. Allow Visual Studio to download the source code from GitHub.
  * ![SourceLink confirmation dialog](https://raw.githubusercontent.com/kontent-ai/kontent-delivery-sdk-net/master/.github/assets/allow_sourcelink_download.png)

**Now you are able to debug the source code of our library without needing to download the source code manually!**

More info about Source Link: https://github.com/dotnet/sourcelink/