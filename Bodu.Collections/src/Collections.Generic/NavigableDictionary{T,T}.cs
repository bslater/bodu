// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionary{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a key-sorted dictionary augmented with order statistics — an <see cref="IDictionary{TKey, TValue}" />
/// that keeps its entries in key comparer order and answers nearest-neighbour ( <see cref="TryGetFloorEntry" /> /
/// <see cref="TryGetCeilingEntry" /> / <see cref="TryGetHigherEntry" /> / <see cref="TryGetLowerEntry" />), rank/select
/// ( <see cref="IndexOfKey" /> / <see cref="GetAt" />), and range-counting queries in O(log n).
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary. Keys must not be <see langword="null" />.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary. Values may be <see langword="null" />.</typeparam>
/// <remarks>
/// <para>
/// <see cref="NavigableDictionary{TKey, TValue}" /> is backed by an order-statistic red-black tree: every node carries
/// the size of its subtree, maintained through all rotations, so positional queries read only size fields on a
/// root-to-node path. The node machinery is the key-value adaptation of the <see cref="NavigableSet{T}" /> core; see
/// <c>Bodu.Collections/docs/navigable-collections-design.md</c> for the backing-structure decision record.
/// </para>
/// <para>
/// Compared with <see cref="SortedDictionary{TKey, TValue}" />, this type adds rank/select ( <see cref="IndexOfKey" />
/// is the zero-based rank of a key; <see cref="GetAt" /> is the entry with the k-th smallest key), O(log n)
/// <see cref="CountInRange" />, the four nearest-neighbour queries, and cheap <see cref="Ascending" /> /
/// <see cref="Descending" /> / <see cref="Range" /> views.
/// </para>
/// <para>
/// Keys are rejected when <see langword="null" />, consistent with the rest of the Bodu collection family; values are
/// unconstrained and may be <see langword="null" />. Keys the comparer orders equal are the same key.
/// </para>
/// <para>
/// This type is not thread-safe.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var quotes = new NavigableDictionary<decimal, string>();
/// quotes.Add(10.00m, "bid");
/// quotes.Add(10.25m, "mid");
/// quotes.Add(10.50m, "ask");
///
/// quotes.TryGetFloorEntry(10.30m, out var floor);   // (10.25, "mid") — greatest key <= 10.30
/// quotes.TryGetCeilingKey(10.30m, out decimal ask); // 10.50 — least key >= 10.30
///
/// int rank = quotes.IndexOfKey(10.25m);             // 1 — zero-based rank in key order
/// var median = quotes.GetAt(quotes.Count / 2);      // (10.25, "mid") — entry with the k-th smallest key
/// int inBand = quotes.CountInRange(10.00m, 10.30m); // 2 — O(log n), no iteration
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(NavigableDictionaryDebugView<,>))]
[Serializable]
public sealed partial class NavigableDictionary<TKey, TValue>
    : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>The comparer that defines the key ordering.</summary>
    private readonly IComparer<TKey> _comparer;

    /// <summary>The root node of the red-black tree, or <see langword="null" /> when the dictionary is empty.</summary>
    private Node? _root;

    /// <summary>The number of entries currently stored in the dictionary.</summary>
    private int _count;

    /// <summary>The structural version, incremented by every mutation to fail-fast in-flight enumerations.</summary>
    private int _version;

    /// <summary>The cached live key view, materialized on first access to <see cref="Keys" />.</summary>
    private KeyCollection? _keys;

    /// <summary>The cached live value view, materialized on first access to <see cref="Values" />.</summary>
    private ValueCollection? _values;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigableDictionary{TKey, TValue}" /> class using the default
    /// comparer.
    /// </summary>
    public NavigableDictionary()
        : this((IComparer<TKey>?)null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigableDictionary{TKey, TValue}" /> class using the specified
    /// comparer.
    /// </summary>
    /// <param name="comparer">
    /// The key ordering comparer, or <see langword="null" /> to use the default comparer.
    /// </param>
    public NavigableDictionary(IComparer<TKey>? comparer)
    {
        _comparer = comparer ?? Comparer<TKey>.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigableDictionary{TKey, TValue}" /> class containing the entries
    /// from <paramref name="source" />, bulk-loaded into a balanced tree.
    /// </summary>
    /// <param name="source">The source entries. Must not be <see langword="null" />.</param>
    /// <param name="comparer">
    /// The key ordering comparer, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <remarks>
    /// The source is sorted by key and built directly into a balanced tree — O(n log n) overall, O(n) after the sort.
    /// Comparer-equal duplicate keys are rejected, matching the <see cref="Dictionary{TKey, TValue}" />
    /// collection-constructor contract.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="source" /> contains a <see langword="null" /> key, or two entries whose keys the comparer orders
    /// equal.
    /// </exception>
    public NavigableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source, IComparer<TKey>? comparer = null)
        : this(comparer)
    {
        ThrowHelper.ThrowIfNull(source);

        KeyValuePair<TKey, TValue>[] entries = source.ToArray();
        foreach (KeyValuePair<TKey, TValue> entry in entries)
        {
            if (entry.Key is null) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_NullCollectionElement, nameof(source));
        }

        if (entries.Length == 0)
            return;

        Array.Sort(entries, (left, right) => _comparer.Compare(left.Key, right.Key));

        for (int i = 1; i < entries.Length; i++)
        {
            if (_comparer.Compare(entries[i].Key, entries[i - 1].Key) == 0)
                throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_DuplicateDictionaryKey, nameof(source));
        }

        _root = BuildFromSortedArray(entries, 0, entries.Length - 1, null);
        _count = entries.Length;
    }

    /// <summary>
    /// Gets the comparer that defines the key ordering.
    /// </summary>
    /// <value>The active key ordering comparer.</value>
    public IComparer<TKey> Comparer => _comparer;

    /// <summary>
    /// Gets the number of entries in the dictionary.
    /// </summary>
    /// <value>The number of key/value pairs currently stored in the dictionary.</value>
    public int Count => _count;

    /// <summary>
    /// Adds the specified key and value to the dictionary.
    /// </summary>
    /// <param name="key">The key of the entry to add. Must not be <see langword="null" />.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <remarks>
    /// This method follows the strict <see cref="Dictionary{TKey, TValue}.Add(TKey, TValue)" /> contract and throws on
    /// a comparer-equal duplicate key. To insert or overwrite without throwing, assign through the indexer; to add only
    /// when absent without throwing, call <see cref="TryAdd" />.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The dictionary already contains a comparer-equal key.</exception>
    public void Add(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);
        if (!TryInsert(key, value)) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_DuplicateDictionaryKey, nameof(key));
    }

    /// <summary>
    /// Attempts to add the specified key and value to the dictionary.
    /// </summary>
    /// <param name="key">The key of the entry to add. Must not be <see langword="null" />.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <returns>
    /// <see langword="true" /> if the entry was added; otherwise, <see langword="false" /> when the dictionary already
    /// contains a comparer-equal key. The stored value is left unchanged on a rejected add.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryAdd(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        return TryInsert(key, value);
    }

    /// <summary>
    /// Removes the entry with the specified key from the dictionary.
    /// </summary>
    /// <param name="key">The key of the entry to remove. Must not be <see langword="null" />.</param>
    /// <returns><see langword="true" /> if the entry was removed; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool Remove(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        Node? node = FindNode(key);
        if (node == null)
            return false;

        RemoveNode(node);

        _count--;
        _version++;
        return true;
    }

    /// <summary>
    /// Removes the entry with the specified key from the dictionary, returning the removed value.
    /// </summary>
    /// <param name="key">The key of the entry to remove. Must not be <see langword="null" />.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the value that was associated with <paramref name="key" />;
    /// otherwise, the default value of <typeparamref name="TValue" />.
    /// </param>
    /// <returns><see langword="true" /> if the entry was removed; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool Remove(TKey key, out TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        Node? node = FindNode(key);
        if (node == null)
        {
            value = default!;
            return false;
        }

        value = node.Value;
        RemoveNode(node);

        _count--;
        _version++;
        return true;
    }

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to retrieve. Must not be <see langword="null" />.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the value associated with <paramref name="key" />; otherwise,
    /// the default value of <typeparamref name="TValue" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the dictionary contains an entry with a comparer-equal key; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetValue(TKey key, out TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        Node? node = FindNode(key);
        if (node == null)
        {
            value = default!;
            return false;
        }

        value = node.Value;
        return true;
    }

    /// <summary>
    /// Determines whether the dictionary contains an entry with the specified key.
    /// </summary>
    /// <param name="key">The key to locate. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if an entry with a comparer-equal key exists; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool ContainsKey(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        return FindNode(key) != null;
    }

    /// <summary>
    /// Determines whether the dictionary contains an entry with the specified value.
    /// </summary>
    /// <param name="value">The value to locate, which may be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if at least one entry stores a value equal to <paramref name="value" /> under
    /// <see cref="EqualityComparer{T}.Default" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This method performs a linear O(n) walk over the entries — values are not indexed. Use
    /// <see cref="ContainsKey" /> for the O(log n) key lookup.
    /// </remarks>
    public bool ContainsValue(TValue value)
    {
        EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;

        Node? node = _root == null ? null : MinimumNode(_root);
        while (node != null)
        {
            if (comparer.Equals(node.Value, value))
                return true;

            node = SuccessorNode(node);
        }

        return false;
    }

    /// <summary>
    /// Removes all entries from the dictionary.
    /// </summary>
    public void Clear()
    {
        _root = null;
        _count = 0;
        _version++;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the dictionary in ascending key order.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> over the dictionary entries.</returns>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <summary>
    /// Inserts a new node for <paramref name="key" /> unless a comparer-equal key already exists.
    /// </summary>
    /// <param name="key">The key to insert.</param>
    /// <param name="value">The value to associate with the key.</param>
    /// <returns><see langword="true" /> if a node was inserted; otherwise, <see langword="false" />.</returns>
    private bool TryInsert(TKey key, TValue value)
    {
        Node? parent = null;
        Node? current = _root;
        int comparison = 0;

        while (current != null)
        {
            comparison = _comparer.Compare(key, current.Key);
            if (comparison == 0)
                return false;

            parent = current;
            current = comparison < 0 ? current.Left : current.Right;
        }

        var node = new Node(key, value) { Parent = parent };

        if (parent == null)
            _root = node;
        else if (comparison < 0)
            parent.Left = node;
        else
            parent.Right = node;

        for (Node? ancestor = parent; ancestor != null; ancestor = ancestor.Parent)
            ancestor.Size++;

        FixAfterInsertion(node);

        _count++;
        _version++;
        return true;
    }
}
