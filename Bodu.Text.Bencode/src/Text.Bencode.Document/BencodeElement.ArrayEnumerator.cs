// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeElement.ArrayEnumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Bencode.Document;

public readonly partial struct BencodeElement
{
    /// <summary>
    /// Enumerates the elements of an array <see cref="BencodeElement" /> in order.
    /// </summary>
    /// <remarks>
    /// The enumerator walks the array's contiguous subtree rows, advancing past each element's whole subtree so that
    /// iteration costs one hop per element regardless of element complexity. It is a mutable <see langword="struct" />;
    /// avoid copying it once enumeration has begun.
    /// </remarks>
    public struct ArrayEnumerator
        : IEnumerable<BencodeElement>, IEnumerator<BencodeElement>
    {
        /// <summary>The owning document.</summary>
        private readonly BencodeDocument _document;

        /// <summary>The row index of the array being enumerated.</summary>
        private readonly int _arrayIndex;

        /// <summary>The total number of elements in the array.</summary>
        private readonly int _count;

        /// <summary>The row index of the element currently being yielded, or <c>-1</c> before the first move.</summary>
        private int _currentRow;

        /// <summary>The number of elements already yielded.</summary>
        private int _consumed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayEnumerator" /> struct.
        /// </summary>
        /// <param name="document">The owning document.</param>
        /// <param name="arrayIndex">The row index of the array being enumerated.</param>
        /// <param name="count">The total number of elements in the array.</param>
        internal ArrayEnumerator(BencodeDocument document, int arrayIndex, int count)
        {
            _document = document;
            _arrayIndex = arrayIndex;
            _count = count;
            _currentRow = -1;
            _consumed = 0;
        }

        /// <summary>
        /// Gets the element at the enumerator's current position.
        /// </summary>
        /// <returns>The current element.</returns>
        public readonly BencodeElement Current =>
            new(_document, _currentRow);

        /// <summary>
        /// Gets the element at the enumerator's current position.
        /// </summary>
        /// <returns>The current element, boxed.</returns>
        readonly object IEnumerator.Current =>
            Current;

        /// <summary>
        /// Returns this enumerator, enabling <see langword="foreach" /> over the array.
        /// </summary>
        /// <returns>This enumerator positioned before the first element.</returns>
        public readonly ArrayEnumerator GetEnumerator()
        {
            ArrayEnumerator copy = this;
            copy.Reset();
            return copy;
        }

        /// <summary>
        /// Returns this enumerator, enabling <see langword="foreach" /> over the array.
        /// </summary>
        /// <returns>This enumerator positioned before the first element.</returns>
        readonly IEnumerator<BencodeElement> IEnumerable<BencodeElement>.GetEnumerator() =>
            GetEnumerator();

        /// <summary>
        /// Returns this enumerator, enabling <see langword="foreach" /> over the array.
        /// </summary>
        /// <returns>This enumerator positioned before the first element.</returns>
        readonly IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        /// <summary>
        /// Advances the enumerator to the next element of the array.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> when the enumerator advanced to a further element; <see langword="false" /> when the
        /// end of the array was reached.
        /// </returns>
        public bool MoveNext()
        {
            if (_consumed >= _count)
                return false;

            _currentRow = _currentRow < 0
                ? _document.FirstChildRow(_arrayIndex)
                : _document.NextSiblingRow(_currentRow);
            _consumed++;
            return true;
        }

        /// <summary>
        /// Resets the enumerator to its initial position, before the first element.
        /// </summary>
        public void Reset()
        {
            _currentRow = -1;
            _consumed = 0;
        }

        /// <summary>
        /// Releases the resources used by the enumerator. This enumerator holds no resources to release.
        /// </summary>
        public readonly void Dispose()
        {
        }
    }
}
