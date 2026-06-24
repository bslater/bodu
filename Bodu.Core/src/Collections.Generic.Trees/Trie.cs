// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Trie.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics;
using Bodu.Collections.Generic.Internal;

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Represents a prefix tree (trie) of string keys and supports efficient prefix queries.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Trie" /> stores keys as paths of characters, so membership and prefix operations cost time
/// proportional to the length of the key rather than to the number of stored keys. Character transitions are keyed
/// by the <see cref="IEqualityComparer{Char}" /> supplied at construction, allowing ordinal or case-insensitive
/// matching. The empty string is a valid key.
/// </para>
/// <para>
/// Enumeration order — through <see cref="GetEnumerator" /> or <see cref="KeysWithPrefix(string)" /> — is unspecified
/// in this version. The trie is not thread-safe for concurrent mutation. For an associative variant that maps keys
/// to values, see <see cref="Trie{TValue}" />.
/// </para>
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(TrieDebugView))]
public sealed partial class Trie
    : IEnumerable<string>, IReadOnlyCollection<string>
{
    private readonly TrieNode<bool> _root = new();
    private int _count;
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="Trie" /> class that is empty and uses ordinal character comparison.
    /// </summary>
    public Trie()
        : this((IEqualityComparer<char>?)null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Trie" /> class that is empty and uses the specified character
    /// comparer.
    /// </summary>
    /// <param name="charComparer">The comparer used to match characters, or <see langword="null" /> to use <see cref="EqualityComparer{Char}.Default" />.</param>
    public Trie(IEqualityComparer<char>? charComparer)
    {
        Comparer = charComparer ?? EqualityComparer<char>.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Trie" /> class containing the specified keys.
    /// </summary>
    /// <param name="keys">The keys to add.</param>
    /// <param name="charComparer">The comparer used to match characters, or <see langword="null" /> to use <see cref="EqualityComparer{Char}.Default" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="keys" /> is <see langword="null" />, or a key is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">A duplicate key is supplied.</exception>
    public Trie(IEnumerable<string> keys, IEqualityComparer<char>? charComparer = null)
        : this(charComparer)
    {
        ThrowHelper.ThrowIfNull(keys);

        foreach (var key in keys)
            Add(key);
    }

    /// <summary>
    /// Gets the number of keys stored in the trie.
    /// </summary>
    /// <value>The number of stored keys.</value>
    /// <returns>The number of stored keys.</returns>
    public int Count => _count;

    /// <summary>
    /// Gets the comparer used to match characters during key lookup.
    /// </summary>
    /// <value>The character comparer supplied at construction, or the default comparer.</value>
    /// <returns>The character comparer in use.</returns>
    public IEqualityComparer<char> Comparer { get; }

    /// <summary>
    /// Adds the specified key to the trie.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <returns><see langword="true" /> if the key was added; <see langword="false" /> if it already existed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool Add(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        var node = TrieCore.GetOrAddNode(_root, key.AsSpan(), Comparer);
        if (node.IsTerminal)
            return false;

        node.IsTerminal = true;
        node.Key = key;
        _count++;
        _version++;
        return true;
    }

    /// <summary>
    /// Determines whether the trie contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true" /> if the key exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool Contains(string key)
    {
        ThrowHelper.ThrowIfNull(key);
        return Contains(key.AsSpan());
    }

    /// <summary>
    /// Determines whether the trie contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true" /> if the key exists; otherwise, <see langword="false" />.</returns>
    public bool Contains(ReadOnlySpan<char> key)
    {
        var node = TrieCore.Find(_root, key);
        return node is not null && node.IsTerminal;
    }

    /// <summary>
    /// Determines whether any key in the trie begins with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to test.</param>
    /// <returns><see langword="true" /> if at least one key begins with <paramref name="prefix" />; otherwise, <see langword="false" />.</returns>
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
    /// <returns><see langword="true" /> if at least one key begins with <paramref name="prefix" />; otherwise, <see langword="false" />.</returns>
    public bool StartsWith(ReadOnlySpan<char> prefix)
    {
        if (prefix.IsEmpty)
            return _count > 0;

        return TrieCore.Find(_root, prefix) is not null;
    }

    /// <summary>
    /// Removes the specified key from the trie.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><see langword="true" /> if the key was found and removed; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool Remove(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!TrieCore.Remove(_root, key.AsSpan()))
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

        var start = TrieCore.Find(_root, prefix.AsSpan());
        return start is null
            ? []
            : EnumerateKeys(start);
    }

    /// <summary>
    /// Removes all keys from the trie.
    /// </summary>
    public void Clear()
    {
        _root.Children = null;
        _root.IsTerminal = false;
        _count = 0;
        _version++;
    }

    /// <summary>
    /// Returns an enumerator that iterates over the keys of the trie.
    /// </summary>
    /// <returns>A struct enumerator over a snapshot of the trie's keys.</returns>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <inheritdoc />
    IEnumerator<string> IEnumerable<string>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Builds a point-in-time array of the trie's keys, used by the enumerator and debugger proxy.
    /// </summary>
    /// <returns>An array containing every key currently stored.</returns>
    internal string[] ToArrayInternal()
    {
        var result = new string[_count];
        var index = 0;
        foreach (var item in TrieCore.EnumerateItems(_root))
            result[index++] = item.Key;

        return result;
    }

    /// <summary>
    /// Projects the key/value enumeration onto its keys.
    /// </summary>
    /// <param name="start">The subtree root to enumerate.</param>
    /// <returns>A lazy sequence of keys.</returns>
    private static IEnumerable<string> EnumerateKeys(TrieNode<bool> start)
    {
        foreach (var item in TrieCore.EnumerateItems(start))
            yield return item.Key;
    }
}
