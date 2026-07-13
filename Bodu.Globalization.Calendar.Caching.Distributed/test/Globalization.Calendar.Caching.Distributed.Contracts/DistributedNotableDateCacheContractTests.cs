// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedNotableDateCacheContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;
using Bodu.Globalization.Calendar.Caching.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar.Caching.Distributed.Contracts;

/// <summary>
/// Runs the shared <see cref="INotableDateCache" /> contract against the distributed cache over an in-process
/// <see cref="MemoryDistributedCache" />, confirming the distributed backend is behaviourally identical to the in-memory
/// and file caches.
/// </summary>
[TestClass]
public sealed class DistributedNotableDateCacheContractTests
    : NotableDateCacheContractTests
{
    /// <inheritdoc />
    protected override INotableDateCache CreateCache()
    {
        IDistributedCache distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new DistributedNotableDateCache(distributedCache, new DistributedNotableDateCacheOptions());
    }
}
