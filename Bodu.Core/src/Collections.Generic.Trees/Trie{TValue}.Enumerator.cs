// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Trie{TValue}.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Trees;

public sealed partial class Trie<TValue>
{
    /// <summary>
    /// Enumerates the key/value pairs of a <see cref="Trie{TValue}" /> over a snapshot captured when the enumerator is
    /// created.
    /// </summary>
    /// <remarks>
    /// The enumerator is fail-fast: if the trie is modified after the enumerator is created, the next call to
    /// <see cref="MoveNext" /> or <see cref="Reset" /> throws <see cref="InvalidOperationException" />.
    /// </remarks>
    public struct Enumerator : IEnumerator<KeyValuePair<string, TValue>>
    {
        /// <summary>The trie being enumerated, used to detect modification through its version counter.</summary>
        private readonly Trie<TValue> _owner;

        /// <summary>The owner's version captured when the enumerator was created, used for fail-fast checks.</summary>
        private readonly int _version;

        /// <summary>The snapshot of key/value pairs captured when the enumerator was created.</summary>
        private readonly KeyValuePair<string, TValue>[] _items;

        /// <summary>The index of the current element, or <c>-1</c> before the first move.</summary>
        private int _index;

        /// <summary>The key/value pair at the current position, or the default value when not on an element.</summary>
        private KeyValuePair<string, TValue> _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct bound to the specified trie.
        /// </summary>
        /// <param name="owner">The trie whose entries are enumerated.</param>
        internal Enumerator(Trie<TValue> owner)
        {
            _owner = owner;
            _version = owner._version;
            _items = owner.ToArrayInternal();
            _index = -1;
            _current = default;
        }

        /// <inheritdoc />
        public readonly KeyValuePair<string, TValue> Current => _current;

        /// <inheritdoc />
        readonly object IEnumerator.Current => _current;

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

            _current = default;
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
            _current = default;
        }

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }
}
