// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionary{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a bidirectional one-to-one dictionary in which every key maps to exactly one value and every value maps
/// back to exactly one key, providing O(1) lookup in both directions.
/// </summary>
/// <typeparam name="TKey">Specifies the type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">Specifies the type of values in the dictionary.</typeparam>
/// <remarks>
/// <para>
/// <see cref="BiDictionary{TKey, TValue}" /> is the .NET analogue of Guava's <c>BiMap</c>, Python's <c>bidict</c>, and
/// Apache Commons' <c>BidiMap</c>: a hash-based map that maintains a second, inverse index from values back to keys so
/// that <see cref="TryGetKey(TValue, out TKey)" />, <see cref="ContainsValue(TValue)" />, and
/// <see cref="RemoveValue(TValue)" /> are O(1) operations rather than linear scans. The two indexes are kept atomically
/// consistent by every mutation.
/// </para>
/// <para>
/// Because the mapping is one-to-one in both directions, adding a pair whose <i>value</i> is already bound to a
/// different key is a conflict. The <see cref="BiDictionaryDuplicateValuePolicy" /> chosen at construction resolves it:
/// <see cref="BiDictionaryDuplicateValuePolicy.Throw" /> (the default) rejects the operation, while
/// <see cref="BiDictionaryDuplicateValuePolicy.Replace" /> evicts the previous binding — the key that held the value is
/// removed — so the new pair wins. Duplicate <i>keys</i> follow the standard
/// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> contract: <see cref="Add(TKey, TValue)" /> throws
/// and the indexer setter re-binds.
/// </para>
/// <para>
/// The <see cref="Inverse" /> property exposes the reversed mapping as a live <see cref="BiDictionary{TValue,
/// TKey}" /> view sharing the same storage: mutations through either view are immediately visible through the other,
/// and <c>Inverse.Inverse</c> returns the original instance. Through the inverse view keys and values swap roles, so
/// the duplicate-value policy there governs conflicts on what the original considers its keys; in both directions the
/// invariant is simply that the mapping stays one-to-one.
/// </para>
/// <para>
/// Both type parameters are constrained by <see langword="notnull" /> — values act as lookup keys in the inverse index,
/// so <see langword="null" /> values cannot be indexed and are rejected at run time just as <see langword="null" />
/// keys are. Custom equality is supported independently on each side via <see cref="KeyComparer" /> and
/// <see cref="ValueComparer" />.
/// </para>
/// <para>
/// Enumeration follows the forward index's unspecified, insertion-biased order (the same non-contractual order as
/// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" />); do not rely on it. Enumerator invalidation also
/// matches the BCL dictionary: adding an entry — through either view — invalidates active enumerators, which then throw
/// <see cref="InvalidOperationException" />; do not mutate the dictionary while enumerating it.
/// </para>
/// <para>
/// <see cref="BiDictionary{TKey, TValue}" /> is not thread-safe. Concurrent reads and writes, including through the
/// <see cref="Inverse" /> view, require external synchronization.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var codes = new BiDictionary<string, int>();
/// codes.Add("AU", 36);
/// codes.Add("NZ", 554);
///
/// int numeric = codes["AU"];                 // 36 — forward lookup
/// codes.TryGetKey(554, out string? alpha);   // "NZ" — O(1) inverse lookup
///
/// // The inverse view shares storage with the original.
/// BiDictionary<int, string> byNumber = codes.Inverse;
/// byNumber.Add(76, "BR");
/// bool present = codes.ContainsKey("BR");    // true — visible through the original
///]]>
/// </code>
/// </example>
/// </remarks>
[DebuggerDisplay("Count: {Count}, DuplicateValuePolicy: {_duplicateValuePolicy}")]
[DebuggerTypeProxy(typeof(BiDictionaryDebugView<,>))]
public sealed partial class BiDictionary<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    /// <summary>The forward index mapping each key to its value; also defines the enumeration order.</summary>
    private readonly Dictionary<TKey, TValue> _forward;

    /// <summary>The inverse index mapping each value back to the key that holds it.</summary>
    private readonly Dictionary<TValue, TKey> _backward;

    /// <summary>The policy applied when a mutation supplies a value already bound to a different key.</summary>
    private readonly BiDictionaryDuplicateValuePolicy _duplicateValuePolicy;

    /// <summary>The lazily created twin exposing the reversed mapping over the same two indexes.</summary>
    private BiDictionary<TValue, TKey>? _inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="BiDictionary{TKey, TValue}" /> class that is empty, rejects
    /// duplicate values, and uses the default comparers.
    /// </summary>
    public BiDictionary()
        : this(BiDictionaryDuplicateValuePolicy.Throw, 0, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiDictionary{TKey, TValue}" /> class that is empty, rejects
    /// duplicate values, has the specified initial capacity, and uses the default comparers.
    /// </summary>
    /// <param name="capacity">The initial number of entries the internal hash tables can hold without resizing.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than zero.</exception>
    public BiDictionary(int capacity)
        : this(BiDictionaryDuplicateValuePolicy.Throw, capacity, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiDictionary{TKey, TValue}" /> class that is empty, rejects
    /// duplicate values, and uses the specified key and value comparers.
    /// </summary>
    /// <param name="keyComparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <param name="valueComparer">
    /// The equality comparer to use for values, or <see langword="null" /> to use the default comparer.
    /// </param>
    public BiDictionary(IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer)
        : this(BiDictionaryDuplicateValuePolicy.Throw, 0, keyComparer, valueComparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiDictionary{TKey, TValue}" /> class that is empty, rejects
    /// duplicate values, and has the specified initial capacity and key and value comparers.
    /// </summary>
    /// <param name="capacity">The initial number of entries the internal hash tables can hold without resizing.</param>
    /// <param name="keyComparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <param name="valueComparer">
    /// The equality comparer to use for values, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than zero.</exception>
    public BiDictionary(int capacity, IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer)
        : this(BiDictionaryDuplicateValuePolicy.Throw, capacity, keyComparer, valueComparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiDictionary{TKey, TValue}" /> class that is empty, uses the
    /// specified duplicate-value policy, and uses the default comparers.
    /// </summary>
    /// <param name="duplicateValuePolicy">
    /// The policy applied when an add or assignment operation supplies a value already bound to a different key.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="duplicateValuePolicy" /> is not a defined <see cref="BiDictionaryDuplicateValuePolicy" /> value.
    /// </exception>
    public BiDictionary(BiDictionaryDuplicateValuePolicy duplicateValuePolicy)
        : this(duplicateValuePolicy, 0, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiDictionary{TKey, TValue}" /> class that is empty, uses the
    /// specified duplicate-value policy, has the specified initial capacity, and uses the default comparers.
    /// </summary>
    /// <param name="duplicateValuePolicy">
    /// The policy applied when an add or assignment operation supplies a value already bound to a different key.
    /// </param>
    /// <param name="capacity">The initial number of entries the internal hash tables can hold without resizing.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="duplicateValuePolicy" /> is not a defined <see cref="BiDictionaryDuplicateValuePolicy" /> value,
    /// or <paramref name="capacity" /> is less than zero.
    /// </exception>
    public BiDictionary(BiDictionaryDuplicateValuePolicy duplicateValuePolicy, int capacity)
        : this(duplicateValuePolicy, capacity, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiDictionary{TKey, TValue}" /> class that is empty, uses the
    /// specified duplicate-value policy, and uses the specified key and value comparers.
    /// </summary>
    /// <param name="duplicateValuePolicy">
    /// The policy applied when an add or assignment operation supplies a value already bound to a different key.
    /// </param>
    /// <param name="keyComparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <param name="valueComparer">
    /// The equality comparer to use for values, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="duplicateValuePolicy" /> is not a defined <see cref="BiDictionaryDuplicateValuePolicy" /> value.
    /// </exception>
    public BiDictionary(
        BiDictionaryDuplicateValuePolicy duplicateValuePolicy,
        IEqualityComparer<TKey>? keyComparer,
        IEqualityComparer<TValue>? valueComparer)
        : this(duplicateValuePolicy, 0, keyComparer, valueComparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiDictionary{TKey, TValue}" /> class that is empty and has the
    /// specified duplicate-value policy, initial capacity, and key and value comparers.
    /// </summary>
    /// <param name="duplicateValuePolicy">
    /// The policy applied when an add or assignment operation supplies a value already bound to a different key.
    /// </param>
    /// <param name="capacity">The initial number of entries the internal hash tables can hold without resizing.</param>
    /// <param name="keyComparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <param name="valueComparer">
    /// The equality comparer to use for values, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="duplicateValuePolicy" /> is not a defined <see cref="BiDictionaryDuplicateValuePolicy" /> value,
    /// or <paramref name="capacity" /> is less than zero.
    /// </exception>
    public BiDictionary(
        BiDictionaryDuplicateValuePolicy duplicateValuePolicy,
        int capacity,
        IEqualityComparer<TKey>? keyComparer,
        IEqualityComparer<TValue>? valueComparer)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(duplicateValuePolicy);
        ThrowHelper.ThrowIfLessThan(capacity, 0);

        _duplicateValuePolicy = duplicateValuePolicy;
        _forward = capacity > 0
            ? new Dictionary<TKey, TValue>(capacity, keyComparer)
            : new Dictionary<TKey, TValue>(keyComparer);
        _backward = capacity > 0
            ? new Dictionary<TValue, TKey>(capacity, valueComparer)
            : new Dictionary<TValue, TKey>(valueComparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiDictionary{TKey, TValue}" /> class as the inverse view of an
    /// existing instance, sharing its two indexes.
    /// </summary>
    /// <param name="forward">The forward index of this view (the twin's inverse index).</param>
    /// <param name="backward">The inverse index of this view (the twin's forward index).</param>
    /// <param name="duplicateValuePolicy">The duplicate-value policy shared with the twin.</param>
    /// <param name="inverse">The twin instance this view was created from.</param>
    private BiDictionary(
        Dictionary<TKey, TValue> forward,
        Dictionary<TValue, TKey> backward,
        BiDictionaryDuplicateValuePolicy duplicateValuePolicy,
        BiDictionary<TValue, TKey> inverse)
    {
        _forward = forward;
        _backward = backward;
        _duplicateValuePolicy = duplicateValuePolicy;
        _inverse = inverse;
    }

    /// <summary>
    /// Gets the policy applied when an add or assignment operation supplies a value that is already bound to a
    /// different key.
    /// </summary>
    /// <value>The duplicate-value policy fixed at construction and shared with the <see cref="Inverse" /> view.</value>
    public BiDictionaryDuplicateValuePolicy DuplicateValuePolicy => _duplicateValuePolicy;

    /// <summary>
    /// Gets the live inverse view of this dictionary, mapping each value back to its key.
    /// </summary>
    /// <value>A <see cref="BiDictionary{TValue, TKey}" /> sharing this instance's storage.</value>
    /// <remarks>
    /// <para>
    /// The view shares the same two indexes as this instance: mutations through either view are immediately visible
    /// through the other, and <c>Inverse.Inverse</c> returns this instance (reference equality). The view is allocated
    /// on first access and cached, so repeated reads return the same instance.
    /// </para>
    /// <para>
    /// Keys and values swap roles through the view — its <see cref="KeyComparer" /> is this instance's
    /// <see cref="ValueComparer" /> and vice versa — and the shared <see cref="DuplicateValuePolicy" /> governs
    /// conflicts on the value side of whichever view is being mutated.
    /// </para>
    /// </remarks>
    public BiDictionary<TValue, TKey> Inverse =>
        _inverse ??= new BiDictionary<TValue, TKey>(_backward, _forward, _duplicateValuePolicy, this);

    /// <summary>
    /// Gets the <see cref="System.Collections.Generic.IEqualityComparer{T}" /> used to determine key equality.
    /// </summary>
    /// <value>The comparer used for key identity in the forward index.</value>
    public IEqualityComparer<TKey> KeyComparer => _forward.Comparer;

    /// <summary>
    /// Gets the <see cref="System.Collections.Generic.IEqualityComparer{T}" /> used to determine value equality.
    /// </summary>
    /// <value>The comparer used for value identity in the inverse index.</value>
    public IEqualityComparer<TValue> ValueComparer => _backward.Comparer;

    /// <summary>
    /// Determines whether the dictionary contains a specific value.
    /// </summary>
    /// <param name="value">The value to locate. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if a key is bound to <paramref name="value" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This is an O(1) operation backed by the inverse index, unlike the O(n) scan of
    /// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}.ContainsValue(TValue)" />. Value equality is
    /// determined by <see cref="ValueComparer" />.
    /// </remarks>
    public bool ContainsValue(TValue value)
    {
        ThrowHelper.ThrowIfNull(value);

        return _backward.ContainsKey(value);
    }

    /// <summary>
    /// Removes the key/value pair whose value equals the specified value.
    /// </summary>
    /// <param name="value">The value of the pair to remove. Must not be <see langword="null" />.</param>
    /// <returns><see langword="true" /> if a pair was found and removed; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This is the value-side counterpart of <see cref="Remove(TKey)" />: it locates the owning key through the inverse
    /// index in O(1) and removes the pair from both indexes atomically.
    /// </remarks>
    public bool RemoveValue(TValue value)
    {
        ThrowHelper.ThrowIfNull(value);

        if (_backward.Remove(value, out TKey? key))
        {
            _forward.Remove(key);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to add the specified key and value to the dictionary without throwing on a conflict.
    /// </summary>
    /// <param name="key">The key of the element to add. Must not be <see langword="null" />.</param>
    /// <param name="value">The value to bind to <paramref name="key" />. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the pair was added; <see langword="false" /> if the key already exists, or if the
    /// value is already bound to a different key and <see cref="DuplicateValuePolicy" /> is
    /// <see cref="BiDictionaryDuplicateValuePolicy.Throw" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This is the non-throwing counterpart of <see cref="Add(TKey, TValue)" />: both conflicts that
    /// <see cref="Add(TKey, TValue)" /> reports by throwing are reported here by returning <see langword="false" />
    /// with the dictionary unchanged. Under <see cref="BiDictionaryDuplicateValuePolicy.Replace" /> a value conflict
    /// instead evicts the previous binding and the method returns <see langword="true" />.
    /// </remarks>
    public bool TryAdd(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(value);

        if (_forward.ContainsKey(key))
            return false;

        if (_backward.TryGetValue(value, out TKey? displacedKey))
        {
            if (_duplicateValuePolicy == BiDictionaryDuplicateValuePolicy.Throw)
                return false;

            _forward.Remove(displacedKey);
        }

        _forward.Add(key, value);
        _backward[value] = key;
        return true;
    }

    /// <summary>
    /// Attempts to retrieve the key bound to the specified value.
    /// </summary>
    /// <param name="value">The value whose key to retrieve. Must not be <see langword="null" />.</param>
    /// <param name="key">
    /// When this method returns, contains the key bound to the specified value, if the value is found; otherwise, the
    /// default value for the type of the key parameter.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if a key is bound to <paramref name="value" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This is the value-side counterpart of <see cref="TryGetValue(TKey, out TValue)" /> and is an O(1) operation
    /// backed by the inverse index. Value equality is determined by <see cref="ValueComparer" />.
    /// </remarks>
    public bool TryGetKey(TValue value, out TKey key)
    {
        ThrowHelper.ThrowIfNull(value);

        return _backward.TryGetValue(value, out key!);
    }
}
