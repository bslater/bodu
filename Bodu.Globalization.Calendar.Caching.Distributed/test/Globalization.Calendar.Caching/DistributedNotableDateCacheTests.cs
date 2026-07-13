// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedNotableDateCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the distributed-specific persistence and option behaviour of <see cref="DistributedNotableDateCache" />.
/// </summary>
[TestClass]
public sealed class DistributedNotableDateCacheTests
{
    /// <summary>The fixed evaluation instant.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that a year stored through one cache instance is read back through a fresh instance over the same
    /// distributed store.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void StoreYear_WhenReadThroughFreshInstance_ShouldPersistAcrossInstances()
    {
        IDistributedCache store = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var options = new DistributedNotableDateCacheOptions();
        var entry = new NotableDateCacheEntry("US", 2026, "v1", new[] { NotableDateCacheTestFactory.ObservedHoliday(2026) }, Now);

        Assert.AreEqual(
            NotableDateCacheWriteStatus.Stored,
            new DistributedNotableDateCache(store, options).StoreYear(entry, TimeSpan.FromDays(30), Now));

        NotableDateCacheEntry? read = new DistributedNotableDateCache(store, options).GetYear("US", 2026, "v1", TimeSpan.FromDays(30), Now);

        Assert.IsNotNull(read);
        Assert.AreEqual(1, read.Occurrences.Count);
        Assert.AreEqual("independence-day", read.Occurrences[0].NotableDateId);
    }

    /// <summary>
    /// Verifies that a white-space key prefix is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_WhenKeyPrefixWhitespace_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            new DistributedNotableDateCacheOptions { KeyPrefix = "   " }.Validate();
        });
    }
}
