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
    /// Enumerates the elements of a <see cref="CircularBuffer{T}" />.
    /// </summary>
    /// <remarks>
    /// <para>Use the <see langword="foreach" /> statement to simplify the enumeration process instead of directly using this enumerator.</para>
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
        /// Initializes a new instance of the <see cref="Enumerator" /> struct.
        /// </summary>
        /// <param name="circularBuffer">The buffer to enumerate.</param>
        internal Enumerator(CircularBuffer<T> circularBuffer)
        {
            this._circularBuffer = circularBuffer;
            this._version = circularBuffer._version;
            this._currentIndex = -1;
            this._current = default!;
            this._iteratedCount = 0;
        }

        /// <inheritdoc />
        public T Current =>
            this._currentIndex == -1
                ? throw new InvalidOperationException(ResourceStrings.InvalidOperation_EnumeratorNotOnElement)
                : this._current;

        /// <inheritdoc />
        object System.Collections.IEnumerator.Current => this.Current!;

        /// <inheritdoc />
        public void Dispose()
        {
            // No unmanaged resources; method provided for interface completeness.
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (this._version != this._circularBuffer._version)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionModified);

            if (this._iteratedCount >= this._circularBuffer._count)
            {
                this._current = default!;
                this._currentIndex = -1; // Ended
                return false;
            }

            this._currentIndex = (this._circularBuffer._head + this._iteratedCount) % this._circularBuffer._capacity;
            this._current = this._circularBuffer._internalBuffer[this._currentIndex];
            this._iteratedCount++;

            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (this._version != this._circularBuffer._version)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionModified);

            this._currentIndex = -1;
            this._current = default!;
            this._iteratedCount = 0;
        }
    }
}
