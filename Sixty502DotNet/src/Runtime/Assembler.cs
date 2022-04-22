using System.Diagnostics;
using System.Reflection;
namespace Sixty502DotNet
{
    /// <summary>
    /// A class containing basic information about the environment and application.
    /// </summary>
    public static class Assembler
    {

        /// <summary>
        /// Gets the version of the assembler.
        /// </summary>
        public static string AssemblerVersion
        {
            get
            {
                var assemblyName = Assembly.GetEntryAssembly()!.GetName();
                return $"Version {assemblyName.Version!.Major}.{assemblyName.Version.Minor} Build {assemblyName.Version.Build}";
            }
        }

        /// <summary>
        /// Gets the assembly product summary, including simple name and version.
        /// </summary>
        public static string ProductSummary
        {
            get
            {
                var product = Assembly.GetEntryAssembly()!.GetName();
                return $"{product.Name} Version {product.Version}";
            }
        }


        /// <summary>
        /// Gets the assembler (product) name.
        /// </summary>
        public static string AssemblerName
        {
            get
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location);
                return $"{versionInfo.Comments}\n{versionInfo.LegalCopyright}";
            }
        }

        /// <summary>
        /// Gets the assembler's simple name, based on the AssemblerName
        /// property.
        /// </summary>
        public static string AssemblerNameSimple
        {
            get
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location);
                return $"{versionInfo.Comments}";
            }
        }
    }
}
