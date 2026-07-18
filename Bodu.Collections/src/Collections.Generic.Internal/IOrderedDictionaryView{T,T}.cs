// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IOrderedDictionaryView{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Provides the seam through which <see cref="DictionaryViewCore" /> reads an order-preserving dictionary, so the
/// key/value view collections, copy loops, and value-containment scans shared by
/// <see cref="EvictingDictionary{TKey, TValue}" /> and <see cref="SequencedDictionary{TKey, TValue}" /> are implemented
/// once instead of being duplicated per type.
/// </summary>
/// <typeparam name="TKey">The type of keys in the owning dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the owning dictionary.</typeparam>
/// <remarks>
/// Implementations expose only what the shared machinery needs: the physically stored count, a pre-copy hook (used by
/// the evicting dictionary to purge expired entries between argument-shape validation and the destination range check),
/// and a boxed ordered enumerator. Everything the two consumers deliberately do differently — enumeration order, expiry
/// filtering, and fail-fast versioning — lives behind the enumerator the implementation returns.
/// </remarks>
internal interface IOrderedDictionaryView<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Gets the number of key/value pairs physically stored in the dictionary, including any entries the ordered
    /// enumeration filters out (for example expired-but-unpurged cache entries).
    /// </summary>
    int ViewCount { get; }

    /// <summary>
    /// Prepares the dictionary for a copy operation. Called after argument-shape validation and before the destination
    /// range is checked against <see cref="ViewCount" />, so the count validated matches the elements written.
    /// </summary>
    /// <remarks>
    /// The evicting dictionary purges expired entries here (raising its eviction events); the sequenced dictionary
    /// performs no work.
    /// </remarks>
    void PrepareViewCopy();

    /// <summary>
    /// Returns an enumerator over the dictionary's key/value pairs in its defining order, with the owning type's
    /// fail-fast versioning and any expiry filtering applied.
    /// </summary>
    /// <returns>A boxed ordered enumerator over the live entries.</returns>
    IEnumerator<KeyValuePair<TKey, TValue>> GetViewEnumerator();
}
