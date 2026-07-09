// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateTableBuilderTests.GetOrAddSeries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates;

public partial class RateTableBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.GetOrAddSeries" /> returns a new empty builder for an unknown key.
    /// </summary>
    [TestMethod]
    public void GetOrAddSeries_WhenSeriesMissing_ShouldCreateAndReturnEmptyBuilder()
    {
        RateTableBuilder table = new();

        RateSeriesBuilder builder = table.GetOrAddSeries(s_usdAud, "RBA");

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
        RateTableBuilder table = new();
        RateSeriesBuilder first = table.GetOrAddSeries(s_usdAud, "RBA");
        RateSeriesBuilder second = table.GetOrAddSeries(s_usdAud, "RBA");

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.GetOrAddSeries" /> validates the provider argument.
    /// </summary>
    [TestMethod]
    public void GetOrAddSeries_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        RateTableBuilder table = new();

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
        RateTableBuilder table = new();
        RateSeriesBuilder rba = table.GetOrAddSeries(s_usdAud, "RBA");
        RateSeriesBuilder ecb = table.GetOrAddSeries(s_usdAud, "ECB");

        Assert.AreNotSame(rba, ecb);
        Assert.AreEqual(2, table.Count);
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.GetOrAddSeries" /> rejects a <see langword="default" /> pair, which
    /// bypasses the pair's own constructor validation and carries null ISO codes.
    /// </summary>
    [TestMethod]
    public void GetOrAddSeries_WhenPairIsDefault_ShouldThrowArgumentException()
    {
        RateTableBuilder table = new();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = table.GetOrAddSeries(default, "RBA");
            },
            "pair");
    }
}
