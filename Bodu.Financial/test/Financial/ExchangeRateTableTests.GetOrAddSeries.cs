// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateTableTests.GetOrAddSeries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateTableTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateTable.GetOrAddSeries" /> returns a new empty builder for an unknown key.
    /// </summary>
    [TestMethod]
    public void GetOrAddSeries_WhenSeriesMissing_ShouldCreateAndReturnEmptyBuilder()
    {
        ExchangeRateTable table = new();

        ExchangeRateSeriesBuilder builder = table.GetOrAddSeries(s_usdAud, "RBA");

        Assert.IsNotNull(builder);
        Assert.IsTrue(builder.IsEmpty);
        Assert.AreEqual(s_usdAud, builder.Pair);
        Assert.AreEqual("RBA", builder.Provider);
        Assert.IsTrue(table.ContainsSeries(s_usdAud, "RBA"));
    }

    /// <summary>
    /// Verifies that repeated calls for the same key return the same builder instance.
    /// </summary>
    [TestMethod]
    public void GetOrAddSeries_WhenCalledTwice_ShouldReturnSameBuilder()
    {
        ExchangeRateTable table = new();
        ExchangeRateSeriesBuilder first = table.GetOrAddSeries(s_usdAud, "RBA");
        ExchangeRateSeriesBuilder second = table.GetOrAddSeries(s_usdAud, "RBA");

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateTable.GetOrAddSeries" /> validates the provider argument.
    /// </summary>
    [TestMethod]
    public void GetOrAddSeries_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        ExchangeRateTable table = new();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = table.GetOrAddSeries(s_usdAud, null!);
            },
            "provider");
    }

    /// <summary>
    /// Verifies that distinct providers for the same pair produce distinct series.
    /// </summary>
    [TestMethod]
    public void GetOrAddSeries_WhenDifferentProvidersForSamePair_ShouldCreateSeparateSeries()
    {
        ExchangeRateTable table = new();
        ExchangeRateSeriesBuilder rba = table.GetOrAddSeries(s_usdAud, "RBA");
        ExchangeRateSeriesBuilder ecb = table.GetOrAddSeries(s_usdAud, "ECB");

        Assert.AreNotSame(rba, ecb);
        Assert.AreEqual(2, table.Count);
    }
}
