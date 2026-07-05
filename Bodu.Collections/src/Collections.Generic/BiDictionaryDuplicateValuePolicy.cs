// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryDuplicateValuePolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Specifies how a <see cref="BiDictionary{TKey, TValue}" /> responds when an add or assignment operation supplies a
/// value that is already bound to a different key.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="BiDictionary{TKey, TValue}" /> maintains a strict one-to-one mapping: every key maps to exactly one
/// value and every value maps back to exactly one key. The policy is consulted whenever a mutation would bind a value
/// that a <i>different</i> key already holds — re-assigning the same value to the same key is never a conflict.
/// </para>
/// <para>
/// The policy is fixed at construction and applies symmetrically through the <see cref="BiDictionary{TKey,
/// TValue}.Inverse" /> view: because keys and values swap roles in the inverse, the policy there governs conflicts on
/// what the original dictionary considers its keys. In both directions the invariant being protected is the same — the
/// mapping stays one-to-one.
/// </para>
/// </remarks>
public enum BiDictionaryDuplicateValuePolicy
{
    /// <summary>
    /// Binding a value already held by a different key is rejected: <see cref="BiDictionary{TKey, TValue}.Add(TKey,
    /// TValue)" /> and the indexer setter throw <see cref="ArgumentException" />, while
    /// <see cref="BiDictionary{TKey, TValue}.TryAdd(TKey, TValue)" /> returns <see langword="false" /> without
    /// modifying state. This is the default and matches the semantics of Guava's <c>BiMap.put</c>.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// Binding a value already held by a different key silently evicts the previous binding — the key that held the
    /// value is removed along with its entry — so the new pair wins and the mapping stays one-to-one. This matches the
    /// semantics of Guava's <c>BiMap.forcePut</c>.
    /// </summary>
    Replace = 1,
}
