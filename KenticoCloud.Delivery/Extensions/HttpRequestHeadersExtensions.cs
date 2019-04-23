using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;

namespace KenticoCloud.Delivery.Extensions
{
    internal static class HttpRequestHeadersExtensions
    {
        private const string SdkTrackingHeaderName = "X-KC-SDKID";
        private const string WaitForLoadingNewContentHeaderName = "X-KC-Wait-For-Loading-New-Content";

        private const string PackageRepositoryHost = "nuget.org";
        
        private static readonly Lazy<string> SdkVersion = new Lazy<String>(GetSdkVersion);
        private static readonly Lazy<string> SdkPackageId = new Lazy<String>(GetSdkPackageId);


        internal static void AddSdkTrackingHeader(this HttpRequestHeaders header)
        {
            header.Add(SdkTrackingHeaderName, $"{PackageRepositoryHost};{SdkPackageId.Value};{SdkVersion.Value}");
        }

        internal static void AddWaitForLoadingNewContentHeader(this HttpRequestHeaders header)
        {
            header.Add(WaitForLoadingNewContentHeaderName, "true");
        }

        internal static void AddAuthorizationHeader(this HttpRequestHeaders header, string scheme, string parameter)
        {
            header.Authorization = new AuthenticationHeaderValue(scheme, parameter);
        }

        private static string GetSdkVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string sdkVersion;

            try
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                sdkVersion = fileVersionInfo.ProductVersion;
            }
            catch (System.IO.FileNotFoundException)
            {
                // Invalid Location path of assembly in Android's Xamarin release mode (unchecked "Use a shared runtime" flag)
                // https://bugzilla.xamarin.com/show_bug.cgi?id=54678
                sdkVersion = "0.0.0";
            }

            return sdkVersion;
        }

        private static string GetSdkPackageId()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var sdkPackageId = assembly.GetName().Name;

            return sdkPackageId;

        }
    }
}
