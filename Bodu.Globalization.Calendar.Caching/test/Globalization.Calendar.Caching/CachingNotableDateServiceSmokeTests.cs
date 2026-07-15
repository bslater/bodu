// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceSmokeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Smoke coverage for the caching layer wrapping a real bundled notable-date service.
/// </summary>
[TestClass]
public sealed class CachingNotableDateServiceSmokeTests
{
    /// <summary>
    /// Verifies that a caching service over a real United States service resolves a public holiday and serves the
    /// repeated query consistently.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Resolve_WhenWrappingRealUnitedStatesService_ShouldServeConsistently()
    {
        INotableDateService inner = AmericasCalendarData.CreateService("US");
        using var caching = new CachingNotableDateService(
            inner,
            new InMemoryNotableDateCache(),
            new NotableDateCachingOptions());

        var year = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        IReadOnlyList<NotableDate> first = caching.Resolve(year, "US");
        IReadOnlyList<NotableDate> second = caching.Resolve(year, "US");

        Assert.IsTrue(first.Count > 0);
        Assert.AreEqual(first.Count, second.Count);
    }
}
