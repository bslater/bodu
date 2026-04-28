// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDeque.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Collections.Generic;

public partial class ArrayDeque<T>
{
    /// <summary>
    /// Enumerates the elements of an <see cref="ArrayDeque{T}"/> in head-to-tail order.
    /// </summary>
    /// <remarks>
    /// <para>Use the <see langword="foreach"/> statement to enumerate the deque rather than using this struct directly.</para>
    /// <para>
    /// The enumerator provides read-only access. Modifying the underlying deque after enumeration begins invalidates
    /// the enumerator and causes <see cref="MoveNext"/> or <see cref="Reset"/> to throw <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct Enumerator :
        System.Collections.Generic.IEnumerator<T>
    {
        private readonly ArrayDeque<T> _deque;
        private readonly int _version;
        private T _current;
        private int _currentIndex;
        private int _iteratedCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator"/> struct.
        /// </summary>
        /// <param name="deque">The deque to enumerate.</param>
        internal Enumerator(ArrayDeque<T> deque)
        {
            _deque = deque;
            _version = deque._storage.Version;
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
            if (_version != _deque._storage.Version)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionModified);

            if (_iteratedCount >= _deque._storage.Count)
            {
                _current = default!;
                _currentIndex = -1;
                return false;
            }

            _currentIndex = (_deque._storage.Head + _iteratedCount) % _deque._storage.Capacity;
            _current = _deque._storage.Array[_currentIndex];
            _iteratedCount++;

            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_version != _deque._storage.Version)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionModified);

            _currentIndex = -1;
            _current = default!;
            _iteratedCount = 0;
        }
    }
}
