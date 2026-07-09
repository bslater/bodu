// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesKeyTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates;

public partial class RateSeriesKeyTests
{

    /// <summary>
    /// Verifies that the validating constructor stores the supplied pair and provider verbatim.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Constructor_WhenInputsValid_ShouldStorePairAndProvider()
    {
        RateSeriesKey key = new(s_usdAud, "RBA");

        Assert.AreEqual(s_usdAud, key.Pair);
        Assert.AreEqual("RBA", key.Provider);
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="default" /> pair, because such an instance bypasses the
    /// pair's own constructor validation and would smuggle null ISO codes into the key.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenPairIsDefault_ShouldThrowArgumentException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new RateSeriesKey(default, "RBA");
            },
            "pair");
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> provider.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new RateSeriesKey(s_usdAud, null!);
            },
            "provider");
    }

    /// <summary>
    /// Verifies that the constructor rejects a white-space-only provider.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenProviderIsWhiteSpace_ShouldThrowArgumentException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new RateSeriesKey(s_usdAud, "   ");
            },
            "provider");
    }
}
