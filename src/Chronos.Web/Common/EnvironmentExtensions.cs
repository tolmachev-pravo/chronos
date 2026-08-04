using System;

namespace Chronos.Web.Common
{
    public static class EnvironmentExtensions
    {
        public static bool IsMock()
        {
            var mockVariable = Environment.GetEnvironmentVariable("ASPNETCORE_IS_MOCK");
            return mockVariable != null
                   && string.Equals(mockVariable, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
