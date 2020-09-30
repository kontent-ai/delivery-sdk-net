using System;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// An attribute allowing library authors to set a custom tracking header in order to be able to gather analytics about their plug-ins.
    /// See https://github.com/Kentico/Home/wiki/Guidelines-for-Kontent-related-tools#analytics for more info.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class DeliverySourceTrackingHeaderAttribute : Attribute
    {
        /// <summary>
        /// Name of the package (e.g. Acme.KenticoKontent.AwesomeTool;1.0.0)
        /// </summary>
        public string PackageName { get; }

        /// <summary>
        /// Major version according to https://semver.org/
        /// </summary>
        public int MajorVersion { get; }

        /// <summary>
        /// Minor version according to https://semver.org/
        /// </summary>
        public int MinorVersion { get; }

        /// <summary>
        /// Patch version according to https://semver.org/
        /// </summary>
        public int PatchVersion { get; }

        /// <summary>
        /// Determines whether or not to load the version from the calling assembly.
        /// </summary>
        public bool LoadFromAssembly { get; }

        /// <summary>
        /// Default constructor ensuring the source information will be extracted from the calling assembly.
        /// </summary>
        public DeliverySourceTrackingHeaderAttribute()
        {
            LoadFromAssembly = true;
        }

        /// <summary>
        /// Constructor allowing to customize the source tracking header.
        /// </summary>
        /// <param name="packageName">Name of the package (e.g. Acme.KenticoKontent.AwesomeTool;1.0.0)</param>
        /// <param name="majorVersion">Major version according to https://semver.org/</param>
        /// <param name="minorVersion">Minor version according to https://semver.org/</param>
        /// <param name="patchVersion">Patch version according to https://semver.org/</param>
        public DeliverySourceTrackingHeaderAttribute(string packageName, int majorVersion, int minorVersion, int patchVersion)
        {
            LoadFromAssembly = false;
            PackageName = packageName;
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            PatchVersion = patchVersion;
        }
    }
}
