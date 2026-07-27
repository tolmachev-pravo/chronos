using System.Reflection;

namespace Pet.Jira.Web.Common
{
    /// <summary>
    /// Version of the running application, stamped at build time from the git tag
    /// (see Directory.Build.props). Falls back to <c>unknown</c> when the attribute is missing.
    /// </summary>
    public static class AppVersion
    {
        private const string Unknown = "unknown";

        static AppVersion()
        {
            var informationalVersion = typeof(AppVersion).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (string.IsNullOrWhiteSpace(informationalVersion))
            {
                Full = Unknown;
                Display = Unknown;
                return;
            }

            Full = informationalVersion;

            var metadataSeparatorIndex = informationalVersion.IndexOf('+');
            Display = metadataSeparatorIndex < 0
                ? $"v{informationalVersion}"
                : $"v{informationalVersion.Substring(0, metadataSeparatorIndex)}";
        }

        /// <summary>
        /// Full informational version including the commit hash,
        /// for example <c>1.6.0+c13046ca9f2f89dfe06342c96f8747a770055ccf</c>.
        /// </summary>
        public static string Full { get; }

        /// <summary>
        /// Version to show in the interface, without build metadata, for example <c>v1.6.0</c>.
        /// </summary>
        public static string Display { get; }
    }
}
