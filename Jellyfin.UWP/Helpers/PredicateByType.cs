﻿using System;
using System.Runtime.CompilerServices;

namespace Jellyfin.UWP.Helpers
{
    internal readonly struct PredicateByType : IPredicate<object>
    {
        /// <summary>
        /// The type of element to match.
        /// </summary>
        private readonly Type type;

        /// <summary>
        /// Initializes a new instance of the <see cref="PredicateByType"/> struct.
        /// </summary>
        /// <param name="type">The type of element to match.</param>
        public PredicateByType(Type type)
        {
            this.type = type;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Match(object element)
        {
            return element.GetType() == this.type;
        }
    }
}
