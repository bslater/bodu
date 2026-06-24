// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Trie.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Trees;

public sealed partial class Trie
{
    /// <summary>
    /// Enumerates the keys of a <see cref="Trie" /> over a snapshot captured when the enumerator is created.
    /// </summary>
    /// <remarks>
    /// The enumerator is fail-fast: if the trie is modified after the enumerator is created, the next call to
    /// <see cref="MoveNext" /> or <see cref="Reset" /> throws <see cref="InvalidOperationException" />.
    /// </remarks>
    public struct Enumerator : IEnumerator<string>
    {
        private readonly Trie _owner;
        private readonly int _version;
        private readonly string[] _items;
        private int _index;
        private string? _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct bound to the specified trie.
        /// </summary>
        /// <param name="owner">The trie whose keys are enumerated.</param>
        internal Enumerator(Trie owner)
        {
            _owner = owner;
            _version = owner._version;
            _items = owner.ToArrayInternal();
            _index = -1;
            _current = null;
        }

        /// <inheritdoc />
        public readonly string Current => _current!;

        /// <inheritdoc />
        readonly object IEnumerator.Current => _current!;

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">
        /// The trie was modified after the enumerator was created.
        /// </exception>
        public bool MoveNext()
        {
            if (_version != _owner._version)
                throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified);

            if (_index + 1 < _items.Length)
            {
                _index++;
                _current = _items[_index];
                return true;
            }

            _current = null;
            return false;
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">
        /// The trie was modified after the enumerator was created.
        /// </exception>
        public void Reset()
        {
            if (_version != _owner._version)
                throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified);

            _index = -1;
            _current = null;
        }

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }
}
