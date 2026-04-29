// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBuffer.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Collections.Generic;

public partial class CircularBuffer<T>
{
    /// <summary>
    /// Enumerates the elements of a <see cref="CircularBuffer{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>Use the <see langword="foreach"/> statement to simplify the enumeration process instead of directly using this enumerator.</para>
    /// <para>
    /// The enumerator provides read-only access to the collection's elements. Modifying the underlying collection while enumerating
    /// invalidates the enumerator.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct Enumerator :
       System.Collections.Generic.IEnumerator<T>
    {
        private readonly CircularBuffer<T> _circularBuffer;
        private readonly int _version;
        private T _current;
        private int _currentIndex;
        private int _iteratedCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator"/> struct.
        /// </summary>
        /// <param name="circularBuffer">The buffer to enumerate.</param>
        internal Enumerator(CircularBuffer<T> circularBuffer)
        {
            _circularBuffer = circularBuffer;
            _version = circularBuffer._storage.Version;
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
            if (_version != _circularBuffer._storage.Version)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionModified);

            if (_iteratedCount >= _circularBuffer._storage.Count)
            {
                _current = default!;
                _currentIndex = -1; // Ended
                return false;
            }

            _currentIndex = (_circularBuffer._storage.Head + _iteratedCount) % _circularBuffer._storage.Capacity;
            _current = _circularBuffer._storage.Array[_currentIndex];
            _iteratedCount++;

            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_version != _circularBuffer._storage.Version)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionModified);

            _currentIndex = -1;
            _current = default!;
            _iteratedCount = 0;
        }
    }
}
