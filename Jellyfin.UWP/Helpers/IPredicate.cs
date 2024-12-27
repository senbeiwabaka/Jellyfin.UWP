namespace Jellyfin.UWP.Helpers
{
    internal interface IPredicate<in T>
        where T : class
    {
        /// <summary>
        /// Performs a match with the current predicate over a target <typeparamref name="T"/> instance.
        /// </summary>
        /// <param name="element">The input element to match.</param>
        /// <returns>Whether the match evaluation was successful.</returns>
        bool Match(T element);
    }
}
