// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrie{T}.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using Bodu.Collections.Generic.Internal;

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrie<TValue>
{
    /// <summary>
    /// Enumerates the key/value pairs of a <see cref="RadixTrie{TValue}" /> by walking the node graph lazily, without
    /// materializing the collection.
    /// </summary>
    /// <remarks>
    /// The enumerator is fail-fast: if the trie is modified after the enumerator is created, the next call to
    /// <see cref="MoveNext" /> or <see cref="Reset" /> throws <see cref="InvalidOperationException" />.
    /// </remarks>
    public struct Enumerator
        : IEnumerator<KeyValuePair<string, TValue>>
    {
        /// <summary>The trie being enumerated, used to detect modification through its version counter.</summary>
        private readonly RadixTrie<TValue> _owner;

        /// <summary>The owner's version captured when the enumerator was created, used for fail-fast checks.</summary>
        private readonly int _version;

        /// <summary>The lazy node-graph walk over the trie's terminal entries.</summary>
        private IEnumerator<KeyValuePair<string, TValue>> _walk;

        /// <summary>The key/value pair at the current position, or the default value when not on an element.</summary>
        private KeyValuePair<string, TValue> _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct bound to the specified trie.
        /// </summary>
        /// <param name="owner">The trie whose entries are enumerated.</param>
        internal Enumerator(RadixTrie<TValue> owner)
        {
            _owner = owner;
            _version = owner._version;
            _walk = RadixTrieCore.EnumerateItems(owner._root).GetEnumerator();
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
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            if (_walk.MoveNext())
            {
                _current = _walk.Current;
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
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            _walk = RadixTrieCore.EnumerateItems(_owner._root).GetEnumerator();
            _current = default;
        }

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }
}
