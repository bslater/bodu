// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullNotableDateCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies that <see cref="NullNotableDateCache" /> stores nothing while enforcing the same argument contract as every
/// other backend.
/// </summary>
[TestClass]
public sealed class NullNotableDateCacheTests
{
    /// <summary>The fixed evaluation instant.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that a stored year is never served back.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenStored_ShouldReturnNull()
    {
        INotableDateCache cache = NullNotableDateCache.Instance;
        cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now), TimeSpan.FromDays(30), Now);

        Assert.IsNull(cache.GetYear("US", 2026, "v1", TimeSpan.FromDays(30), Now));
    }

    /// <summary>
    /// Verifies that a store reports it was skipped.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenCalled_ShouldReturnSkipped()
    {
        Assert.AreEqual(
            NotableDateCacheWriteStatus.Skipped,
            NullNotableDateCache.Instance.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now), TimeSpan.FromDays(30), Now));
    }

    /// <summary>
    /// Verifies that a null entry is rejected even though nothing is stored.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenEntryNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NullNotableDateCache.Instance.StoreYear(null!, TimeSpan.FromDays(30), Now);
        });
    }
}
