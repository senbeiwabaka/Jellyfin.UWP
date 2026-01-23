using System.Diagnostics.CodeAnalysis;

namespace Jellyfin;

[ExcludeFromCodeCoverage]
public static class DebugHelpers
{
    public static bool IsDebugRelease
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
