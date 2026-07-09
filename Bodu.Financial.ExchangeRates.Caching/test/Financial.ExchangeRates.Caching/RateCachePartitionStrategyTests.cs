// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCachePartitionStrategyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies <see cref="RateCachePartitionStrategy" />: the partition keys and ranges of the built-in
/// granularities, the single-file mode, and custom strategies.
/// </summary>
[TestClass]
public sealed partial class RateCachePartitionStrategyTests
{
    /// <summary>
    /// Verifies that the single strategy is not partitioned while the calendar strategies are.
    /// </summary>
    /// <param name="isPartitioned">The expected partitioning flag.</param>
    /// <param name="which">The strategy under test, identified by name.</param>
    [TestMethod]
    [DataRow(false, "Single")]
    [DataRow(true, "Yearly")]
    [DataRow(true, "Monthly")]
    [DataRow(true, "Daily")]
    public void IsPartitioned_ForBuiltInStrategy_ShouldMatchGranularity(bool isPartitioned, string which) =>
        Assert.AreEqual(isPartitioned, Strategy(which).IsPartitioned);

    /// <summary>
    /// Verifies the smoke path that the monthly strategy keys a date by its calendar month.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GetPartitionKey_ForMonthly_ShouldKeyByMonth() =>
        Assert.AreEqual("2023-06", RateCachePartitionStrategy.Monthly.GetPartitionKey(new DateOnly(2023, 6, 15)));

    /// <summary>
    /// Resolves a built-in strategy by name for data-driven tests.
    /// </summary>
    /// <param name="which">The strategy name.</param>
    /// <returns>The named built-in strategy.</returns>
    private static RateCachePartitionStrategy Strategy(string which) => which switch
    {
        "Single" => RateCachePartitionStrategy.Single,
        "Yearly" => RateCachePartitionStrategy.Yearly,
        "Monthly" => RateCachePartitionStrategy.Monthly,
        "Daily" => RateCachePartitionStrategy.Daily,
        _ => throw new ArgumentOutOfRangeException(nameof(which)),
    };
}
