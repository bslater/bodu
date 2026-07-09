// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyPairTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates;

[TestClass]
public partial class CurrencyPairTests
{
    /// <summary>
    /// Verifies that the constructor stores both currencies verbatim.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Constructor_WhenInputsValid_ShouldStoreBothCodes()
    {
        CurrencyPair pair = new(CurrencyCode.USD, CurrencyCode.AUD);

        Assert.AreEqual(CurrencyCode.USD, pair.From);
        Assert.AreEqual(CurrencyCode.AUD, pair.To);
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyPair.Inverse" /> swaps the source and destination currencies.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenCalled_ShouldReturnPairWithCodesSwapped()
    {
        CurrencyPair pair = new(CurrencyCode.USD, CurrencyCode.AUD);

        CurrencyPair inverse = pair.Inverse();

        Assert.AreEqual(CurrencyCode.AUD, inverse.From);
        Assert.AreEqual(CurrencyCode.USD, inverse.To);
    }

    /// <summary>
    /// Verifies that two pairs with identical currencies compare equal via the generated record-struct equality.
    /// </summary>
    [TestMethod]
    public void Equality_WhenComponentsMatch_ShouldReportEqual()
    {
        CurrencyPair a = new(CurrencyCode.USD, CurrencyCode.AUD);
        CurrencyPair b = new(CurrencyCode.USD, CurrencyCode.AUD);

        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that two pairs whose components differ in direction compare unequal.
    /// </summary>
    [TestMethod]
    public void Equality_WhenDirectionDiffers_ShouldReportUnequal()
    {
        CurrencyPair a = new(CurrencyCode.USD, CurrencyCode.AUD);
        CurrencyPair b = new(CurrencyCode.AUD, CurrencyCode.USD);

        Assert.AreNotEqual(a, b);
    }

    /// <summary>
    /// Verifies that a <see cref="CurrencyCode.None" /> source currency throws
    /// <see cref="ArgumentOutOfRangeException" /> with the parameter name <c>from</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenFromIsNone_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new CurrencyPair(CurrencyCode.None, CurrencyCode.AUD);
            },
            "from");
    }

    /// <summary>
    /// Verifies that a <see cref="CurrencyCode.None" /> destination currency throws
    /// <see cref="ArgumentOutOfRangeException" /> with the parameter name <c>to</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenToIsNone_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new CurrencyPair(CurrencyCode.USD, CurrencyCode.None);
            },
            "to");
    }

    /// <summary>
    /// Verifies that an undefined currency cast throws <see cref="ArgumentOutOfRangeException" /> on either parameter.
    /// </summary>
    [TestMethod]
    [DataRow(true, "from")]
    [DataRow(false, "to")]
    public void Constructor_WhenCodeUndefined_ShouldThrowArgumentOutOfRangeExceptionWithExpectedParamName(
        bool invalidFrom,
        string expectedParamName)
    {
        CurrencyCode from = invalidFrom ? (CurrencyCode)9999 : CurrencyCode.USD;
        CurrencyCode to = invalidFrom ? CurrencyCode.AUD : (CurrencyCode)9999;

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new CurrencyPair(from, to);
            },
            expectedParamName);
    }
}
