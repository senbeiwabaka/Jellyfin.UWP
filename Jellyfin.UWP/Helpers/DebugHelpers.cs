using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.UWP.Helpers;

[ExcludeFromCodeCoverage]
internal static class DebugHelpers
{
    internal static bool IsDebugRelease
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}
