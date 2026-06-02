// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesKeyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

[TestClass]
public partial class ExchangeRateSeriesKeyTests
{
    private static readonly ExchangeRatePair s_usdAud = new("USD", "AUD");

    /// <summary>
    /// Verifies that the validating constructor stores the supplied pair and provider verbatim.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Constructor_WhenInputsValid_ShouldStorePairAndProvider()
    {
        ExchangeRateSeriesKey key = new(s_usdAud, "RBA");

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
                _ = new ExchangeRateSeriesKey(default, "RBA");
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
                _ = new ExchangeRateSeriesKey(s_usdAud, null!);
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
                _ = new ExchangeRateSeriesKey(s_usdAud, "   ");
            },
            "provider");
    }

    /// <summary>
    /// Verifies that two validated keys with identical components compare equal via the generated record-struct
    /// equality.
    /// </summary>
    [TestMethod]
    public void Equality_WhenComponentsMatch_ShouldReportEqual()
    {
        ExchangeRateSeriesKey a = new(s_usdAud, "RBA");
        ExchangeRateSeriesKey b = new(s_usdAud, "RBA");

        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }
}
