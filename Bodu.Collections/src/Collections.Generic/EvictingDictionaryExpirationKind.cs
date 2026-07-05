// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryExpirationKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Specifies how an <see cref="EvictingDictionary{TKey, TValue}" /> measures an entry's time-to-live when expiration is
/// configured through <see cref="EvictingDictionaryExpiration" />.
/// </summary>
/// <remarks>
/// <para>
/// The kind determines the point from which an entry's time-to-live is counted. Under <see cref="Absolute" /> the
/// countdown starts when the entry is added (or its value is updated) and is unaffected by reads. Under
/// <see cref="Sliding" /> every successful read access — <see cref="EvictingDictionary{TKey, TValue}.TryGetValue" />,
/// the indexer getter, and <see cref="EvictingDictionary{TKey, TValue}.ContainsKey" /> — restarts the countdown,
/// keeping actively read entries alive indefinitely.
/// </para>
/// <para>
/// Enumeration never refreshes an entry's lifetime under either kind.
/// </para>
/// </remarks>
public enum EvictingDictionaryExpirationKind
{
    /// <summary>
    /// The entry expires a fixed interval after it was added or last updated; read accesses do not extend its lifetime.
    /// </summary>
    Absolute = 0,

    /// <summary>
    /// The entry's expiration deadline is refreshed on every successful read access ( <c>TryGetValue</c>, the indexer
    /// getter, or <c>ContainsKey</c>), so the entry expires only after remaining unread for its full time-to-live.
    /// </summary>
    Sliding = 1,
}
