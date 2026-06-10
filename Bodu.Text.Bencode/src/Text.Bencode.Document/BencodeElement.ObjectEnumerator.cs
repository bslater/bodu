// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeElement.ObjectEnumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Bencode.Document;

public readonly partial struct BencodeElement
{
    /// <summary>
    /// Enumerates the key/value pairs of an object <see cref="BencodeElement" /> in stored order, mirroring the role of
    /// <see cref="System.Text.Json.JsonElement.ObjectEnumerator" />.
    /// </summary>
    /// <remarks>
    /// The enumerator walks the object's contiguous subtree rows, advancing from each pair to the next by skipping the
    /// whole of the current value's subtree. It is a mutable <see langword="struct" />; avoid copying it once
    /// enumeration has begun.
    /// </remarks>
    public struct ObjectEnumerator
        : IEnumerable<BencodeProperty>, IEnumerator<BencodeProperty>
    {
        /// <summary>
        /// The owning document.
        /// </summary>
        private readonly BencodeDocument _document;

        /// <summary>
        /// The row index of the object being enumerated.
        /// </summary>
        private readonly int _objectIndex;

        /// <summary>
        /// The total number of key/value pairs in the object.
        /// </summary>
        private readonly int _count;

        /// <summary>
        /// The row index of the key of the pair currently being yielded, or <c>-1</c> before the first move.
        /// </summary>
        private int _currentKeyRow;

        /// <summary>
        /// The row index where the next pair's key begins.
        /// </summary>
        private int _nextKeyRow;

        /// <summary>
        /// The number of pairs already yielded.
        /// </summary>
        private int _consumed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectEnumerator" /> struct.
        /// </summary>
        /// <param name="document">The owning document.</param>
        /// <param name="objectIndex">The row index of the object being enumerated.</param>
        /// <param name="count">The total number of key/value pairs in the object.</param>
        internal ObjectEnumerator(BencodeDocument document, int objectIndex, int count)
        {
            _document = document;
            _objectIndex = objectIndex;
            _count = count;
            _currentKeyRow = -1;
            _nextKeyRow = document.FirstChildRow(objectIndex);
            _consumed = 0;
        }

        /// <summary>
        /// Gets the property at the enumerator's current position.
        /// </summary>
        /// <returns>The current property.</returns>
        public readonly BencodeProperty Current
        {
            get
            {
                (string name, int valueRow, _) = _document.GetPair(_currentKeyRow);
                return new BencodeProperty(name, new BencodeElement(_document, valueRow));
            }
        }

        /// <summary>
        /// Gets the property at the enumerator's current position.
        /// </summary>
        /// <returns>The current property, boxed.</returns>
        readonly object IEnumerator.Current =>
            Current;

        /// <summary>
        /// Returns this enumerator, enabling <see langword="foreach" /> over the object's properties.
        /// </summary>
        /// <returns>This enumerator positioned before the first property.</returns>
        public readonly ObjectEnumerator GetEnumerator()
        {
            ObjectEnumerator copy = this;
            copy.Reset();
            return copy;
        }

        /// <summary>
        /// Returns this enumerator, enabling <see langword="foreach" /> over the object's properties.
        /// </summary>
        /// <returns>This enumerator positioned before the first property.</returns>
        readonly IEnumerator<BencodeProperty> IEnumerable<BencodeProperty>.GetEnumerator() =>
            GetEnumerator();

        /// <summary>
        /// Returns this enumerator, enabling <see langword="foreach" /> over the object's properties.
        /// </summary>
        /// <returns>This enumerator positioned before the first property.</returns>
        readonly IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        /// <summary>
        /// Advances the enumerator to the next property of the object.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> when the enumerator advanced to a further property; <see langword="false" /> when
        /// the end of the object was reached.
        /// </returns>
        public bool MoveNext()
        {
            if (_consumed >= _count)
                return false;

            _currentKeyRow = _nextKeyRow;
            (_, _, _nextKeyRow) = _document.GetPair(_currentKeyRow);
            _consumed++;
            return true;
        }

        /// <summary>
        /// Resets the enumerator to its initial position, before the first property.
        /// </summary>
        public void Reset()
        {
            _currentKeyRow = -1;
            _nextKeyRow = _document.FirstChildRow(_objectIndex);
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
