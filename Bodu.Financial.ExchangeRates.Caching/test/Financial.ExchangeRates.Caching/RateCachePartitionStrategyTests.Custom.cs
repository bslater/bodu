// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCachePartitionStrategyTests.Custom.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class RateCachePartitionStrategyTests
{
    /// <summary>
    /// Verifies that a custom strategy routes a date through the supplied key and range selectors and reports itself as
    /// partitioned.
    /// </summary>
    [TestMethod]
    public void Custom_WhenGivenSelectors_ShouldUseThem()
    {
        var quarterly = RateCachePartitionStrategy.Custom(
            date => $"{date.Year}-Q{((date.Month - 1) / 3) + 1}",
            date =>
            {
                int firstMonth = (((date.Month - 1) / 3) * 3) + 1;
                var start = new DateOnly(date.Year, firstMonth, 1);
                return (start, start.AddMonths(3).AddDays(-1));
            });

        Assert.IsTrue(quarterly.IsPartitioned);
        Assert.AreEqual("2023-Q2", quarterly.GetPartitionKey(new DateOnly(2023, 5, 20)));
        Assert.AreEqual((new DateOnly(2023, 4, 1), new DateOnly(2023, 6, 30)), quarterly.GetPartitionRange(new DateOnly(2023, 5, 20)));
    }

    /// <summary>
    /// Verifies that a custom strategy rejects a <see langword="null" /> key selector.
    /// </summary>
    [TestMethod]
    public void Custom_WhenKeySelectorIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = RateCachePartitionStrategy.Custom(null!, _ => (DateOnly.MinValue, DateOnly.MaxValue));
        });

        Assert.AreEqual("keySelector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a custom strategy rejects a <see langword="null" /> range selector.
    /// </summary>
    [TestMethod]
    public void Custom_WhenRangeSelectorIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = RateCachePartitionStrategy.Custom(_ => string.Empty, null!);
        });

        Assert.AreEqual("rangeSelector", ex.ParamName);
    }
}
