// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionary{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a live, read-through view over an ordered list of underlying dictionaries in which the first layer
/// containing a key supplies its value, and all writes are applied to the first layer only.
/// </summary>
/// <typeparam name="TKey">Specifies the type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">Specifies the type of values in the dictionary.</typeparam>
/// <remarks>
/// <para>
/// <see cref="LayeredDictionary{TKey, TValue}" /> is the .NET analogue of Python's <c>collections.ChainMap</c>: it
/// groups several dictionaries into a single updatable view without copying them. Lookups search the layers in order
/// and the <i>first</i> layer containing the key wins — an entry in an earlier layer <i>shadows</i> any entry with the
/// same key in later layers. The same first-wins precedence model governs <c>Bodu.Text.Configuration</c>'s resolver
/// chain, where earlier configuration sources take precedence over later ones.
/// </para>
/// <para>
/// The layer <i>list</i> is fixed at construction, but each layer is held by reference and remains fully live:
/// mutations made directly to an underlying dictionary are immediately visible through the view. All mutations made <i>through</i>
/// the view — <see cref="Add(TKey, TValue)" />, the indexer setter, <see cref="Remove(TKey)" />, and
/// <see cref="Clear" /> — affect the first layer only, exactly matching Python <c>ChainMap</c> semantics. In
/// particular, <see cref="Remove(TKey)" /> returns <see langword="false" /> for a key that exists only in deeper
/// layers, and removing a first-layer entry that shadowed a deeper one makes the deeper value visible again (the
/// "unshadowing" behaviour).
/// </para>
/// <para>
/// <see cref="Count" />, <see cref="Keys" />, <see cref="Values" />, and enumeration present the merged view: distinct
/// keys across all layers with first-wins values. These operations walk every layer and track seen keys, so they cost
/// O(n) in the total number of entries across all layers — <see cref="Count" /> is <i>not</i> a cached O(1) property.
/// <see cref="Keys" /> and <see cref="Values" /> return snapshots materialized at the time of the call; enumeration is
/// lazy and reflects the layers as it walks them.
/// </para>
/// <para>
/// The optional key comparer supplied at construction is used only by the view's own distinct-key logic (the seen-key
/// set behind <see cref="Count" /> and enumeration). Each underlying dictionary continues to use its own comparer for
/// its lookups, so mismatched comparers can produce surprising shadowing — for example, a case-insensitive first layer
/// can shadow keys the view's case-sensitive comparer considers distinct. Prefer constructing the view and all layers
/// with the same comparer.
/// </para>
/// <para>
/// The non-generic <see cref="System.Collections.IDictionary" /> and <see cref="System.Collections.ICollection" />
/// surfaces are deliberately not implemented: this type is a lightweight view over existing dictionaries rather than a
/// stand-alone collection, and the legacy interfaces add no value to it.
/// </para>
/// <para>
/// <see cref="LayeredDictionary{TKey, TValue}" /> is not thread-safe. Concurrent reads and writes, including direct
/// mutations of the underlying layers, require external synchronization.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var overrides = new Dictionary<string, string>();
/// var defaults = new Dictionary<string, string> { ["colour"] = "blue", ["size"] = "medium" };
///
/// var settings = new LayeredDictionary<string, string>(overrides, defaults);
///
/// string colour = settings["colour"];    // "blue" — falls through to the defaults layer
/// settings["colour"] = "red";            // writes to the overrides layer, shadowing the default
/// colour = settings["colour"];           // "red" — the first layer wins
///
/// settings.Remove("colour");             // removes from the overrides layer only…
/// colour = settings["colour"];           // "blue" — the default is unshadowed
///]]>
/// </code>
/// </example>
/// </remarks>
[DebuggerDisplay("Count = {Count}, Layers = {_layers.Length}")]
[DebuggerTypeProxy(typeof(LayeredDictionaryDebugView<,>))]
public sealed partial class LayeredDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>The ordered layer list fixed at construction; index 0 is the write layer and wins on read.</summary>
    private readonly IDictionary<TKey, TValue>[] _layers;

    /// <summary>The read-only wrapper exposed through <see cref="Layers" />.</summary>
    private readonly ReadOnlyCollection<IDictionary<TKey, TValue>> _layersView;

    /// <summary>The comparer used by the view's distinct-key logic (seen-key tracking and <see cref="Count" />).</summary>
    private readonly IEqualityComparer<TKey> _comparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayeredDictionary{TKey, TValue}" /> class over the specified
    /// layers, using the default key comparer for the view's distinct-key logic.
    /// </summary>
    /// <param name="layers">
    /// The underlying dictionaries in precedence order; the first layer wins on read and receives all writes. Must not
    /// be <see langword="null" />, must not be empty, and must not contain <see langword="null" /> elements.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="layers" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="layers" /> is empty, or contains a <see langword="null" /> element.
    /// </exception>
    public LayeredDictionary(params IDictionary<TKey, TValue>[] layers)
        : this((IEnumerable<IDictionary<TKey, TValue>>)layers, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LayeredDictionary{TKey, TValue}" /> class over the specified layer
    /// sequence, using the default key comparer for the view's distinct-key logic.
    /// </summary>
    /// <param name="layers">
    /// The underlying dictionaries in precedence order; the first layer wins on read and receives all writes. Must not
    /// be <see langword="null" />, must not be empty, and must not contain <see langword="null" /> elements.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="layers" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="layers" /> is empty, or contains a <see langword="null" /> element.
    /// </exception>
    public LayeredDictionary(IEnumerable<IDictionary<TKey, TValue>> layers)
        : this(layers, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LayeredDictionary{TKey, TValue}" /> class over the specified layer
    /// sequence, using the specified key comparer for the view's distinct-key logic.
    /// </summary>
    /// <param name="layers">
    /// The underlying dictionaries in precedence order; the first layer wins on read and receives all writes. Must not
    /// be <see langword="null" />, must not be empty, and must not contain <see langword="null" /> elements.
    /// </param>
    /// <param name="comparer">
    /// The equality comparer used by the view's distinct-key logic, or <see langword="null" /> to use the default
    /// comparer.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="layers" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="layers" /> is empty, or contains a <see langword="null" /> element.
    /// </exception>
    /// <remarks>
    /// The sequence is materialized defensively: later changes to <paramref name="layers" /> itself do not affect the
    /// view, but the referenced dictionaries remain live. The comparer governs only how the <i>view</i> deduplicates
    /// keys across layers; each underlying dictionary keeps using its own comparer for lookups.
    /// </remarks>
    public LayeredDictionary(IEnumerable<IDictionary<TKey, TValue>> layers, IEqualityComparer<TKey>? comparer)
    {
        ThrowHelper.ThrowIfNull(layers);
        IDictionary<TKey, TValue>[] materialized = [.. layers];
        ThrowHelper.ThrowIfCollectionIsEmpty(materialized, nameof(layers));
        if (Array.IndexOf(materialized, null) >= 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_NullCollectionElement, nameof(layers));

        _layers = materialized;
        _layersView = new ReadOnlyCollection<IDictionary<TKey, TValue>>(materialized);
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
    }

    /// <summary>
    /// Gets the ordered list of underlying layers this view reads through.
    /// </summary>
    /// <value>
    /// A read-only list of the layer dictionaries in precedence order; index 0 is the write layer and wins on read.
    /// </value>
    /// <remarks>
    /// The list itself is fixed at construction, but each element is the live underlying dictionary — mutating a layer
    /// directly is immediately visible through the view.
    /// </remarks>
    public IReadOnlyList<IDictionary<TKey, TValue>> Layers => _layersView;

    /// <summary>
    /// Gets the <see cref="System.Collections.Generic.IEqualityComparer{T}" /> used by the view's distinct-key logic.
    /// </summary>
    /// <value>The comparer used to deduplicate keys across layers during <see cref="Count" /> and enumeration.</value>
    /// <remarks>
    /// This comparer does not influence lookups — each underlying layer resolves keys with its own comparer. When the
    /// layers and the view disagree on key equality, shadowing may not match the view's notion of "same key".
    /// </remarks>
    public IEqualityComparer<TKey> Comparer => _comparer;
}
