// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedCacheEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

/// <summary>
/// The root JSON shape persisted for a single currency pair in the distributed cache, wrapping the cached rate rows
/// together with the recorded coverage windows so both halves of the pair's state are carried in one entry.
/// </summary>
/// <remarks>
/// One <see cref="DistributedCacheEntry" /> is serialized per pair under a single distributed-cache key. The two
/// collections are independent halves of the pair's state: storing rates replaces only <see cref="Rates" /> and
/// recording coverage replaces only <see cref="Coverage" />, each preserving the other half on write. An entry written
/// before either half existed deserializes its missing collection to an empty list so a partially populated blob keeps
/// working.
/// </remarks>
internal sealed class DistributedCacheEntry
{
    /// <summary>
    /// Gets or sets the cached rate rows for the pair.
    /// </summary>
    /// <returns>The cached rate rows, never <see langword="null" /> after deserialization.</returns>
    public List<DistributedCacheRate> Rates { get; set; } = new();

    /// <summary>
    /// Gets or sets the recorded coverage windows for the pair.
    /// </summary>
    /// <returns>The coverage windows recording which date ranges were fetched.</returns>
    public List<DistributedCacheCoverage> Coverage { get; set; } = new();
}
