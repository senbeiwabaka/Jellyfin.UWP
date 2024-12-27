using System.Runtime.CompilerServices;

namespace Jellyfin.UWP.Helpers
{
    internal readonly struct PredicateByAny<T> : IPredicate<T>
        where T : class
    {
        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Match(T element)
        {
            return true;
        }
    }
}
