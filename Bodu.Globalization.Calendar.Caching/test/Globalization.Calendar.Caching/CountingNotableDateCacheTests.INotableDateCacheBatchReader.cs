// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingNotableDateCacheTests.INotableDateCacheBatchReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

public sealed partial class CountingNotableDateCacheTests
{
    /// <summary>
    /// Verifies that batch reads made through <see cref="INotableDateCacheBatchReader" /> from several threads at once
    /// are each counted.
    /// </summary>
    [TestMethod]
    public void GetYears_WhenCalledFromSeveralThreadsAtOnce_ShouldCountEveryCall()
    {
        var cache = new CountingNotableDateCache(supportsBatch: true);
        var reader = (INotableDateCacheBatchReader)cache.AsCache();

        CountingNotableDateServiceTests.RunOnThreadsAtOnce(() => reader.GetYears("US", 2026, 2027, "fixed", Ttl, Now));

        Assert.AreEqual(CountingNotableDateServiceTests.TotalCalls, cache.GetYearsCount);
    }
}
