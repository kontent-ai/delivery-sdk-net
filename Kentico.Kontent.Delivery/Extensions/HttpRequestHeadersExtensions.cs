using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Kentico.Kontent.Delivery.Extensions
{
    internal static class HttpRequestHeadersExtensions
    {
        private const string SdkTrackingHeaderName = "X-KC-SDKID";
        private const string SourceTrackingHeaderName = "X-KC-SOURCE";
        private const string WaitForLoadingNewContentHeaderName = "X-KC-Wait-For-Loading-New-Content";
        private const string ContinuationHeaderName = "X-Continuation";

        private const string PackageRepositoryHost = "nuget.org";

        private static readonly Lazy<string> Sdk = new Lazy<string>(GetSdk);
        private static readonly Lazy<string> Source = new Lazy<string>(GetSource);

        internal static void AddSdkTrackingHeader(this HttpRequestHeaders headers)
        {
            headers.Add(SdkTrackingHeaderName, Sdk.Value);
        }

        /// <summary>
        /// Adds a tracking header according to https://github.com/Kentico/Home/wiki/Guidelines-for-Kontent-related-tools#analytics
        /// </summary>
        /// <param name="headers">Collection of headers</param>
        internal static void AddSourceTrackingHeader(this HttpRequestHeaders headers)
        {
            var source = Source.Value;
            if (source != null)
            {
                headers.Add(SourceTrackingHeaderName, source);
            }
        }

        internal static void AddWaitForLoadingNewContentHeader(this HttpRequestHeaders headers)
        {
            headers.Add(WaitForLoadingNewContentHeaderName, "true");
        }

        internal static void AddAuthorizationHeader(this HttpRequestHeaders headers, string scheme, string parameter)
        {
            headers.Authorization = new AuthenticationHeaderValue(scheme, parameter);
        }

        internal static void AddContinuationHeader(this HttpRequestHeaders headers, string continuation)
        {
            headers.Add(ContinuationHeaderName, continuation);
        }

        internal static string GetProducVersion(this Assembly assembly)
        {
            string sdkVersion;

            try
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                sdkVersion = fileVersionInfo.ProductVersion;
            }
            catch (FileNotFoundException)
            {
                // Invalid Location path of assembly in Android's Xamarin release mode (unchecked "Use a shared runtime" flag)
                // https://bugzilla.xamarin.com/show_bug.cgi?id=54678
                sdkVersion = "0.0.0";
            }

            return sdkVersion;
        }

        internal static string GetSdk()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var sdkVersion = assembly.GetProducVersion();
            var sdkPackageId = assembly.GetName().Name;

            return $"{PackageRepositoryHost};{sdkPackageId};{sdkVersion}";
        }

        internal static string GetSource()
        {
            Assembly originatingAssembly = GetOriginatingAssembly();

            var attribute = originatingAssembly.GetCustomAttributes<DeliverySourceTrackingHeaderAttribute>().FirstOrDefault();
            if (attribute != null)
            {
                return GenerateSourceTrackingHeaderValue(originatingAssembly, attribute);
            }
            return null;
        }

        internal static string GenerateSourceTrackingHeaderValue(Assembly originatingAssembly, DeliverySourceTrackingHeaderAttribute attribute)
        {
            string packageName;
            string version;
            if (attribute.LoadFromAssembly)
            {
                packageName = attribute.PackageName ?? originatingAssembly.GetName().Name;
                version = originatingAssembly.GetProducVersion();
            }
            else
            {
                packageName = attribute.PackageName;
                string preRelease = attribute.PreReleaseLabel == null ? "" : $"-{attribute.PreReleaseLabel}";
                version = $"{attribute.MajorVersion}.{attribute.MinorVersion}.{attribute.PatchVersion}{preRelease}";
            }
            return $"{packageName};{version}";
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Assembly GetOriginatingAssembly()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var callerAssemblies = new StackTrace().GetFrames()
                        .Select(x => x.GetMethod().ReflectedType?.Assembly).Distinct().OfType<Assembly>()
                        .Where(x => x.GetReferencedAssemblies().Any(y => y.FullName == executingAssembly.FullName));
            var originatingAssembly = callerAssemblies.Last();
            return originatingAssembly;
        }
    }
}
