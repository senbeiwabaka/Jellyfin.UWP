using System;
using System.Runtime.CompilerServices;

namespace Jellyfin.UWP.Helpers
{
    internal readonly struct PredicateByFunc<T> : IPredicate<T>
        where T : class
    {
        /// <summary>
        /// The predicatee to use to match items.
        /// </summary>
        private readonly Func<T, bool> predicate;

        /// <summary>
        /// Initializes a new instance of the <see cref="PredicateByFunc{T}"/> struct.
        /// </summary>
        /// <param name="predicate">The predicatee to use to match items.</param>
        public PredicateByFunc(Func<T, bool> predicate)
        {
            this.predicate = predicate;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Match(T element)
        {
            return this.predicate(element);
        }
    }
}
