namespace Jellyfin.UWP.Helpers
{
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
}
