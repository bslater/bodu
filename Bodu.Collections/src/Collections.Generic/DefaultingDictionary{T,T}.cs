// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultingDictionary{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a dictionary whose indexer getter materializes missing entries on demand: reading an absent key invokes a
/// value factory, stores the produced value, and returns it.
/// </summary>
/// <typeparam name="TKey">Specifies the type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">Specifies the type of values in the dictionary.</typeparam>
/// <remarks>
/// <para>
/// <see cref="DefaultingDictionary{TKey, TValue}" /> is the .NET analogue of Python's <c>collections.defaultdict</c>:
/// the <see cref="ValueFactory" /> delegate fixed at construction plays the role of <c>default_factory</c>, and —
/// exactly as in Python, where <c>__missing__</c> fires only for <c>d[key]</c> — <i>only the indexer getter</i>
/// materializes defaults. <see cref="TryGetValue(TKey, out TValue)" />, <see cref="ContainsKey(TKey)" />,
/// <see cref="Remove(TKey)" />, <see cref="Count" />, and enumeration observe only entries that have actually been
/// stored; none of them invoke the factory.
/// </para>
/// <para>
/// This differs from the <c>GetOrAdd</c> extension methods on
/// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" /> in <c>Bodu.Core</c>, which stay the lightweight
/// per-call-site option: there the factory is supplied at each call, whereas here the default-producing policy is baked
/// into the dictionary itself so every plain indexer read applies it.
/// </para>
/// <para>
/// Reentrancy contract: when the factory itself mutates the dictionary — including adding an entry for the very key
/// being materialized — the stored result is always the factory's <i>return value</i>. Any value the factory assigned
/// to that key during its own execution is overwritten before the indexer returns. This keeps the contract
/// deterministic: <c>dictionary[key]</c> on a miss always stores and returns what the factory returned.
/// </para>
/// <para>
/// <typeparamref name="TValue" /> is unconstrained, so a factory returning <see langword="null" /> for a reference or
/// nullable value type stores <see langword="null" /> as-is; the key is then present with a <see langword="null" />
/// value and the factory is not invoked for it again.
/// </para>
/// <para>
/// <see cref="DefaultingDictionary{TKey, TValue}" /> is not thread-safe. The indexer's check-invoke-store sequence is
/// not atomic; concurrent access requires external synchronization.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var groups = new DefaultingDictionary<string, List<int>>(_ => new List<int>());
///
/// groups["odd"].Add(1);     // miss — the factory creates the list, it is stored, then mutated
/// groups["odd"].Add(3);     // hit — the stored list is returned; the factory is not invoked
///
/// bool has = groups.ContainsKey("even");   // false — lookups never materialize defaults
/// int count = groups["odd"].Count;         // 2
///]]>
/// </code>
/// </example>
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(DefaultingDictionaryDebugView<,>))]
public sealed partial class DefaultingDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>The inner dictionary holding every actually-stored entry.</summary>
    private readonly Dictionary<TKey, TValue> _inner;

    /// <summary>The delegate invoked by the indexer getter to produce a value for a missing key.</summary>
    private readonly Func<TKey, TValue> _valueFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultingDictionary{TKey, TValue}" /> class that is empty, uses
    /// the specified value factory, and uses the default key comparer.
    /// </summary>
    /// <param name="valueFactory">
    /// The delegate invoked to produce the value for a key read through the indexer while absent. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="valueFactory" /> is <see langword="null" />.</exception>
    public DefaultingDictionary(Func<TKey, TValue> valueFactory)
        : this(valueFactory, 0, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultingDictionary{TKey, TValue}" /> class that is empty, uses
    /// the specified value factory, and uses the specified key comparer.
    /// </summary>
    /// <param name="valueFactory">
    /// The delegate invoked to produce the value for a key read through the indexer while absent. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="valueFactory" /> is <see langword="null" />.</exception>
    public DefaultingDictionary(Func<TKey, TValue> valueFactory, IEqualityComparer<TKey>? comparer)
        : this(valueFactory, 0, comparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultingDictionary{TKey, TValue}" /> class that is empty, uses
    /// the specified value factory, and has the specified initial capacity and key comparer.
    /// </summary>
    /// <param name="valueFactory">
    /// The delegate invoked to produce the value for a key read through the indexer while absent. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="capacity">The initial number of entries the inner dictionary can hold without resizing.</param>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="valueFactory" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than zero.</exception>
    public DefaultingDictionary(Func<TKey, TValue> valueFactory, int capacity, IEqualityComparer<TKey>? comparer)
    {
        ThrowHelper.ThrowIfNull(valueFactory);
        ThrowHelper.ThrowIfLessThan(capacity, 0);

        _valueFactory = valueFactory;
        _inner = new Dictionary<TKey, TValue>(capacity, comparer);
    }

    /// <summary>
    /// Gets the <see cref="System.Collections.Generic.IEqualityComparer{T}" /> used to determine key equality.
    /// </summary>
    /// <value>The comparer used for key identity in the inner dictionary.</value>
    public IEqualityComparer<TKey> Comparer => _inner.Comparer;

    /// <summary>
    /// Gets the delegate invoked by the indexer getter to produce the value for a missing key.
    /// </summary>
    /// <value>The value factory fixed at construction.</value>
    public Func<TKey, TValue> ValueFactory => _valueFactory;
}
