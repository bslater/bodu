// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCacheContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Caching.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed.Contracts;

/// <summary>
/// Runs the shared <see cref="IRateCache" /> contract against the distributed cache, each test backed by a
/// fresh in-memory <see cref="MemoryDistributedCache" /> (an in-process <see cref="IDistributedCache" />; no Redis
/// server is used). Confirms the distributed backend is behaviourally identical to the in-memory, TOML, and SQLite
/// caches.
/// </summary>
[TestClass]
public sealed class DistributedRateCacheContractTests
    : RateCacheContractTests<DistributedRateCache>
{
    /// <inheritdoc />
    protected override DistributedRateCache CreateCache()
    {
        IDistributedCache distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new DistributedRateCache(distributedCache, new DistributedRateCacheOptions { Provider = Provider });
    }
}
