// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollection.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollection<T>
{
    /// <summary>
    /// Enumerates the elements of a <see cref="RingBackedCollection{T}"/> in head-to-tail logical order.
    /// </summary>
    /// <remarks>
    /// <para>Use the <see langword="foreach"/> statement to enumerate the collection rather than using this struct directly.</para>
    /// <para>
    /// The enumerator provides read-only access. Modifying the underlying collection after enumeration begins
    /// invalidates the enumerator and causes <see cref="MoveNext"/> or <see cref="Reset"/> to throw
    /// <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct Enumerator :
        System.Collections.Generic.IEnumerator<T>
    {
        private readonly RingBackedCollection<T> _collection;
        private readonly int _version;
        private T _current;
        private int _currentIndex;
        private int _iteratedCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator"/> struct.
        /// </summary>
        /// <param name="collection">The collection to enumerate.</param>
        internal Enumerator(RingBackedCollection<T> collection)
        {
            _collection = collection;
            _version = collection._version;
            _currentIndex = -1;
            _current = default!;
            _iteratedCount = 0;
        }

        /// <inheritdoc />
        public T Current =>
            _currentIndex == -1
                ? throw new InvalidOperationException(ResourceStrings.InvalidOperation_EnumeratorNotOnElement)
                : _current;

        /// <inheritdoc />
        object System.Collections.IEnumerator.Current => Current!;

        /// <inheritdoc />
        public void Dispose()
        {
            // No unmanaged resources; method provided for interface completeness.
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _collection._version)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionModified);

            if (_iteratedCount >= _collection._count)
            {
                _current = default!;
                _currentIndex = -1; // Ended
                return false;
            }

            _currentIndex = (_collection._head + _iteratedCount) % _collection._array.Length;
            _current = _collection._array[_currentIndex];
            _iteratedCount++;

            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_version != _collection._version)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionModified);

            _currentIndex = -1;
            _current = default!;
            _iteratedCount = 0;
        }
    }
}
