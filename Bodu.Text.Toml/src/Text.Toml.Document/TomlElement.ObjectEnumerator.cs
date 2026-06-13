// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlElement.ObjectEnumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Toml.Document;

public readonly partial struct TomlElement
{
    /// <summary>
    /// Enumerates the key/value pairs of a table <see cref="TomlElement" /> in stored order.
    /// </summary>
    /// <remarks>
    /// The enumerator walks the table's contiguous subtree rows, advancing from each pair to the next by skipping the
    /// whole of the current value's subtree. It is a mutable <see langword="struct" />; avoid copying it once
    /// enumeration has begun.
    /// </remarks>
    public struct ObjectEnumerator
        : IEnumerable<TomlProperty>, IEnumerator<TomlProperty>
    {
        /// <summary>
        /// The owning document.
        /// </summary>
        private readonly TomlDocument _document;

        /// <summary>
        /// The row index of the table being enumerated.
        /// </summary>
        private readonly int _tableIndex;

        /// <summary>
        /// The total number of key/value pairs in the table.
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
        /// <param name="tableIndex">The row index of the table being enumerated.</param>
        /// <param name="count">The total number of key/value pairs in the table.</param>
        internal ObjectEnumerator(TomlDocument document, int tableIndex, int count)
        {
            _document = document;
            _tableIndex = tableIndex;
            _count = count;
            _currentKeyRow = -1;
            _nextKeyRow = document.FirstChildRow(tableIndex);
            _consumed = 0;
        }

        /// <summary>
        /// Gets the property at the enumerator's current position.
        /// </summary>
        /// <returns>The current property.</returns>
        public readonly TomlProperty Current
        {
            get
            {
                (var name, var valueRow, _) = _document.GetPair(_currentKeyRow);
                return new TomlProperty(name, new TomlElement(_document, valueRow));
            }
        }

        /// <summary>
        /// Gets the property at the enumerator's current position.
        /// </summary>
        /// <returns>The current property, boxed.</returns>
        readonly object IEnumerator.Current =>
            Current;

        /// <summary>
        /// Returns this enumerator, enabling <see langword="foreach" /> over the table's properties.
        /// </summary>
        /// <returns>This enumerator positioned before the first property.</returns>
        public readonly ObjectEnumerator GetEnumerator()
        {
            ObjectEnumerator copy = this;
            copy.Reset();
            return copy;
        }

        /// <summary>
        /// Returns this enumerator, enabling <see langword="foreach" /> over the table's properties.
        /// </summary>
        /// <returns>This enumerator positioned before the first property.</returns>
        readonly IEnumerator<TomlProperty> IEnumerable<TomlProperty>.GetEnumerator() =>
            GetEnumerator();

        /// <summary>
        /// Returns this enumerator, enabling <see langword="foreach" /> over the table's properties.
        /// </summary>
        /// <returns>This enumerator positioned before the first property.</returns>
        readonly IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        /// <summary>
        /// Advances the enumerator to the next property of the table.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> when the enumerator advanced to a further property; <see langword="false" /> when
        /// the end of the table was reached.
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
            _nextKeyRow = _document.FirstChildRow(_tableIndex);
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
