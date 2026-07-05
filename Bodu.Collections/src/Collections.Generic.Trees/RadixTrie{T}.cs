// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrie{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics;
using Bodu.Collections.Generic.Internal;

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Represents a path-compressed prefix tree (radix trie) that maps string keys to values and supports efficient prefix
/// queries.
/// </summary>
/// <typeparam name="TValue">The type of the values associated with each key.</typeparam>
/// <remarks>
/// <para>
/// A <see cref="RadixTrie{TValue}" /> stores keys as paths of multi-character edge labels: runs of characters with no
/// branching share a single node, so node count is proportional to the number of stored keys rather than to their total
/// length. Insertion splits an edge at the point of divergence and removal re-fuses any single-child pass-through node
/// it leaves behind. Character transitions and label matching are keyed by the <see cref="IEqualityComparer{Char}" />
/// supplied at construction, allowing ordinal or case-insensitive matching.
/// </para>
/// <para>
/// The public surface mirrors <see cref="Trie{TValue}" /> member-for-member, so the two types are drop-in
/// interchangeable; prefer <see cref="RadixTrie{TValue}" /> when keys share long unbranching runs (URLs, file paths,
/// identifiers). The empty string is a valid key. Enumeration order — whether through <see cref="GetEnumerator" />,
/// <see cref="KeysWithPrefix(string)" />, or <see cref="ItemsWithPrefix(string)" /> — is unspecified in this version.
/// The trie is not thread-safe for concurrent mutation.
/// </para>
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(RadixTrieDebugView<>))]
public sealed partial class RadixTrie<TValue>
    : IEnumerable<KeyValuePair<string, TValue>>, IReadOnlyCollection<KeyValuePair<string, TValue>>
{
    /// <summary>The root node of the trie; only its child edges are meaningful, not its value slot.</summary>
    private readonly RadixTrieNode<TValue> _root = new();

    /// <summary>The number of key/value pairs currently stored in the trie.</summary>
    private int _count;

    /// <summary>A modification counter used to detect mutation during enumeration.</summary>
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadixTrie{TValue}" /> class that is empty and uses ordinal
    /// character comparison.
    /// </summary>
    public RadixTrie()
        : this((IEqualityComparer<char>?)null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RadixTrie{TValue}" /> class that is empty and uses the specified
    /// character comparer.
    /// </summary>
    /// <param name="charComparer">
    /// The comparer used to match characters, or <see langword="null" /> to use
    /// <see cref="EqualityComparer{Char}.Default" />.
    /// </param>
    public RadixTrie(IEqualityComparer<char>? charComparer)
    {
        Comparer = charComparer ?? EqualityComparer<char>.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RadixTrie{TValue}" /> class containing the specified key/value
    /// pairs.
    /// </summary>
    /// <param name="items">The key/value pairs to add.</param>
    /// <param name="charComparer">
    /// The comparer used to match characters, or <see langword="null" /> to use
    /// <see cref="EqualityComparer{Char}.Default" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items" /> is <see langword="null" />, or a key is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">A duplicate key is supplied.</exception>
    public RadixTrie(IEnumerable<KeyValuePair<string, TValue>> items, IEqualityComparer<char>? charComparer = null)
        : this(charComparer)
    {
        ThrowHelper.ThrowIfNull(items);

        foreach (KeyValuePair<string, TValue> item in items)
            Add(item.Key, item.Value);
    }

    /// <summary>
    /// Gets the number of keys stored in the trie.
    /// </summary>
    /// <value>The number of stored keys.</value>
    public int Count => _count;

    /// <summary>
    /// Gets the comparer used to match characters during key lookup.
    /// </summary>
    /// <value>The character comparer supplied at construction, or the default comparer.</value>
    public IEqualityComparer<char> Comparer { get; }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value is retrieved or assigned.</param>
    /// <value>The value associated with <paramref name="key" />.</value>
    /// <returns>The value currently mapped to <paramref name="key" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">The key does not exist when read.</exception>
    public TValue this[string key]
    {
        get
        {
            ThrowHelper.ThrowIfNull(key);

            RadixTrieNode<TValue>? node = RadixTrieCore.Find(_root, key.AsSpan(), Comparer);
            if (node?.IsTerminal != true)
                throw new KeyNotFoundException();

            return node.Value;
        }

        set
        {
            ThrowHelper.ThrowIfNull(key);
            Set(key, value);
        }
    }

    /// <summary>
    /// Adds the specified key and value to the trie.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to associate with the key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The key already exists.</exception>
    public void Add(string key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);
        Add(key.AsSpan(), value);
    }

    /// <summary>
    /// Adds the specified key and value to the trie.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to associate with the key.</param>
    /// <exception cref="ArgumentException">The key already exists.</exception>
    public void Add(ReadOnlySpan<char> key, TValue value)
    {
        RadixTrieNode<TValue> node = RadixTrieCore.GetOrAddNode(_root, key, Comparer);
        if (node.IsTerminal)
            throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_DuplicateDictionaryKey, nameof(key));

        node.IsTerminal = true;
        node.Key = new string(key);
        node.Value = value;
        _count++;
        _version++;
    }

    /// <summary>
    /// Attempts to add the specified key and value to the trie.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to associate with the key.</param>
    /// <returns><see langword="true" /> if the key was added; <see langword="false" /> if it already existed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryAdd(string key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        RadixTrieNode<TValue> node = RadixTrieCore.GetOrAddNode(_root, key.AsSpan(), Comparer);
        if (node.IsTerminal)
            return false;

        node.IsTerminal = true;
        node.Key = key;
        node.Value = value;
        _count++;
        _version++;
        return true;
    }

    /// <summary>
    /// Adds or updates the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to add or update.</param>
    /// <param name="value">The value to associate with the key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public void Set(string key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        RadixTrieNode<TValue> node = RadixTrieCore.GetOrAddNode(_root, key.AsSpan(), Comparer);
        if (!node.IsTerminal)
        {
            node.IsTerminal = true;
            node.Key = key;
            _count++;
        }

        node.Value = value;
        _version++;
    }

    /// <summary>
    /// Determines whether the trie contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true" /> if the key exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool ContainsKey(string key)
    {
        ThrowHelper.ThrowIfNull(key);
        return ContainsKey(key.AsSpan());
    }

    /// <summary>
    /// Determines whether the trie contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true" /> if the key exists; otherwise, <see langword="false" />.</returns>
    public bool ContainsKey(ReadOnlySpan<char> key)
    {
        RadixTrieNode<TValue>? node = RadixTrieCore.Find(_root, key, Comparer);
        return node?.IsTerminal == true;
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">When this method returns, contains the value if found; otherwise, the default value.</param>
    /// <returns><see langword="true" /> if the key was found; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetValue(string key, out TValue value)
    {
        ThrowHelper.ThrowIfNull(key);
        return TryGetValue(key.AsSpan(), out value);
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">When this method returns, contains the value if found; otherwise, the default value.</param>
    /// <returns><see langword="true" /> if the key was found; otherwise, <see langword="false" />.</returns>
    public bool TryGetValue(ReadOnlySpan<char> key, out TValue value)
    {
        RadixTrieNode<TValue>? node = RadixTrieCore.Find(_root, key, Comparer);
        if (node?.IsTerminal == true)
        {
            value = node.Value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Determines whether any key in the trie begins with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to test.</param>
    /// <returns>
    /// <see langword="true" /> if at least one key begins with <paramref name="prefix" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="prefix" /> is <see langword="null" />.</exception>
    public bool StartsWith(string prefix)
    {
        ThrowHelper.ThrowIfNull(prefix);
        return StartsWith(prefix.AsSpan());
    }

    /// <summary>
    /// Determines whether any key in the trie begins with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to test.</param>
    /// <returns>
    /// <see langword="true" /> if at least one key begins with <paramref name="prefix" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool StartsWith(ReadOnlySpan<char> prefix)
    {
        if (prefix.IsEmpty)
            return _count > 0;

        return RadixTrieCore.FindSubtree(_root, prefix, Comparer) is not null;
    }

    /// <summary>
    /// Removes the specified key from the trie.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>
    /// <see langword="true" /> if the key was found and removed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool Remove(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!RadixTrieCore.Remove(_root, key.AsSpan(), Comparer))
            return false;

        _count--;
        _version++;
        return true;
    }

    /// <summary>
    /// Returns the keys in the trie that begin with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match.</param>
    /// <returns>A lazily evaluated sequence of matching keys, in unspecified order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="prefix" /> is <see langword="null" />.</exception>
    public IEnumerable<string> KeysWithPrefix(string prefix)
    {
        ThrowHelper.ThrowIfNull(prefix);

        RadixTrieNode<TValue>? start = RadixTrieCore.FindSubtree(_root, prefix.AsSpan(), Comparer);
        return start is null
            ? []
            : EnumerateKeys(start);
    }

    /// <summary>
    /// Returns the key/value pairs in the trie whose keys begin with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match.</param>
    /// <returns>A lazily evaluated sequence of matching key/value pairs, in unspecified order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="prefix" /> is <see langword="null" />.</exception>
    public IEnumerable<KeyValuePair<string, TValue>> ItemsWithPrefix(string prefix)
    {
        ThrowHelper.ThrowIfNull(prefix);

        RadixTrieNode<TValue>? start = RadixTrieCore.FindSubtree(_root, prefix.AsSpan(), Comparer);
        return start is null
            ? []
            : RadixTrieCore.EnumerateItems(start);
    }

    /// <summary>
    /// Removes all keys from the trie.
    /// </summary>
    public void Clear()
    {
        _root.Children = null;
        _root.IsTerminal = false;
        _root.Value = default!;
        _count = 0;
        _version++;
    }

    /// <summary>
    /// Returns an enumerator that iterates over the key/value pairs of the trie.
    /// </summary>
    /// <returns>A struct enumerator over a snapshot of the trie's entries.</returns>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <inheritdoc />
    IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Builds a point-in-time array of the trie's entries, used by the enumerator and debugger proxy.
    /// </summary>
    /// <returns>An array containing every key/value pair currently stored.</returns>
    internal KeyValuePair<string, TValue>[] ToArrayInternal()
    {
        var result = new KeyValuePair<string, TValue>[_count];
        int index = 0;
        foreach (KeyValuePair<string, TValue> item in RadixTrieCore.EnumerateItems(_root))
            result[index++] = item;

        return result;
    }

    /// <summary>
    /// Projects the key/value enumeration onto its keys.
    /// </summary>
    /// <param name="start">The subtree root to enumerate.</param>
    /// <returns>A lazy sequence of keys.</returns>
    private static IEnumerable<string> EnumerateKeys(RadixTrieNode<TValue> start)
    {
        foreach (KeyValuePair<string, TValue> item in RadixTrieCore.EnumerateItems(start))
            yield return item.Key;
    }
}
