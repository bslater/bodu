// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheFileLayoutTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies <see cref="ExchangeRateCacheFileLayout" />: the built-in layouts' partitioning, directory, and file-name
/// rules, custom layouts, and segment sanitization.
/// </summary>
[TestClass]
public sealed partial class ExchangeRateCacheFileLayoutTests
{
    /// <summary>The currency pair used by the tests.</summary>
    private static readonly CurrencyPair Pair = new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <summary>
    /// Verifies that the single-file layout is not partitioned while the calendar layouts are.
    /// </summary>
    /// <param name="isPartitioned">The expected partitioning flag.</param>
    /// <param name="which">The layout under test, identified by name.</param>
    [TestMethod]
    [DataRow(false, "SingleFile")]
    [DataRow(true, "Yearly")]
    [DataRow(true, "Monthly")]
    [DataRow(true, "Daily")]
    public void IsPartitioned_ForBuiltInLayout_ShouldMatchGranularity(bool isPartitioned, string which) =>
        Assert.AreEqual(isPartitioned, Layout(which).IsPartitioned);

    /// <summary>
    /// Verifies that a built-in layout exposes the matching partition strategy.
    /// </summary>
    [TestMethod]
    public void PartitionStrategy_ForMonthly_ShouldBeMonthlyStrategy() =>
        Assert.AreSame(ExchangeRateCachePartitionStrategy.Monthly, ExchangeRateCacheFileLayout.Monthly.PartitionStrategy);

    /// <summary>
    /// Verifies the smoke path that the single-file layout resolves the default per-provider file name.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void ResolveFileName_ForSingleFile_ShouldUsePairToken() =>
        Assert.AreEqual("AUDUSD.toml", ExchangeRateCacheFileLayout.SingleFile.ResolveFileName("RBA", Pair, string.Empty, ".toml"));

    /// <summary>
    /// Resolves a built-in layout by name for data-driven tests.
    /// </summary>
    /// <param name="which">The layout name.</param>
    /// <returns>The named built-in layout.</returns>
    private static ExchangeRateCacheFileLayout Layout(string which) => which switch
    {
        "SingleFile" => ExchangeRateCacheFileLayout.SingleFile,
        "Yearly" => ExchangeRateCacheFileLayout.Yearly,
        "Monthly" => ExchangeRateCacheFileLayout.Monthly,
        "Daily" => ExchangeRateCacheFileLayout.Daily,
        _ => throw new ArgumentOutOfRangeException(nameof(which)),
    };
}
