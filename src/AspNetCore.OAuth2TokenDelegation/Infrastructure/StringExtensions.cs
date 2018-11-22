using System.Diagnostics;

namespace AspNetCore.OAuth2TokenDelegation.Infrastructure
{
    internal static class StringExtensions
    {
        [DebuggerStepThrough]
        public static bool IsMissing(this string value) => string.IsNullOrWhiteSpace(value);

        [DebuggerStepThrough]
        public static bool IsPresent(this string value) => !string.IsNullOrWhiteSpace(value);
    }
}
