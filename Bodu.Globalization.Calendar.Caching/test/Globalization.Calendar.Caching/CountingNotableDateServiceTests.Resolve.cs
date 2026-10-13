// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingNotableDateServiceTests.Resolve.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

public sealed partial class CountingNotableDateServiceTests
{
    /// <summary>
    /// Verifies that resolutions made from several threads at once are each counted, so a test that resolves years in
    /// the background reads the exact number of calls.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenCalledFromSeveralThreadsAtOnce_ShouldCountEveryCall()
    {
        var service = new CountingNotableDateService();
        var range = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        RunOnThreadsAtOnce(() => service.Resolve(range, "US"));

        Assert.AreEqual(TotalCalls, service.ResolveCount);
    }
}
