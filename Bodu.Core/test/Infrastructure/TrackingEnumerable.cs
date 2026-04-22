// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TrackingEnumerable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using System.Collections;

namespace Bodu.Infrastructure
{
    /// <summary>
    /// A test utility that wraps an <see cref="IEnumerable{T}" /> and tracks enumeration behavior.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public sealed class TrackingEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> source;
        private readonly Action onEnumerate;
        private readonly Action<int>? onItemAccess;

        private int itemsEnumerated = 0;
        private bool enumeratorCreated = false;
        private bool enforceSingleEnumeration;

        /// <summary>
        /// Gets whether the enumerable has been enumerated at least once.
        /// </summary>
        public bool HasEnumerated => enumeratorCreated;

        /// <summary>
        /// Gets the number of items enumerated (only tracked in first enumeration).
        /// </summary>
        public int ItemsEnumerated => itemsEnumerated;

        /// <summary>
        /// Enables enforcement of one-time enumeration.
        /// </summary>
        public TrackingEnumerable<T> EnforceSingleEnumeration()
        {
            enforceSingleEnumeration = true;
            return this;
        }

        /// <summary>
        /// Creates a new <see cref="TrackingEnumerable{T}" /> instance.
        /// </summary>
        /// <param name="source">The underlying source sequence.</param>
        /// <param name="onEnumerate">An optional callback invoked when enumeration begins.</param>
        /// <param name="onItemAccess">An optional callback for each item accessed.</param>
        public TrackingEnumerable(IEnumerable<T> source, Action? onEnumerate = null, Action<int>? onItemAccess = null)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.onEnumerate = onEnumerate ?? (() => { });
            this.onItemAccess = onItemAccess;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (enumeratorCreated && enforceSingleEnumeration)
                throw new InvalidOperationException("Sequence cannot be enumerated more than once.");

            enumeratorCreated = true;
            onEnumerate();

            int index = 0;
            foreach (var item in source)
            {
                onItemAccess?.Invoke(index);
                itemsEnumerated = ++index;
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}