// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingNotableDateCacheTests.GetYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

public sealed partial class CountingNotableDateCacheTests
{
    /// <summary>
    /// Verifies that per-year reads made from several threads at once are each counted.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenCalledFromSeveralThreadsAtOnce_ShouldCountEveryCall()
    {
        var cache = new CountingNotableDateCache(supportsBatch: false);

        CountingNotableDateServiceTests.RunOnThreadsAtOnce(() => cache.GetYear("US", 2026, "fixed", Ttl, Now));

        Assert.AreEqual(CountingNotableDateServiceTests.TotalCalls, cache.GetYearCount);
    }
}
