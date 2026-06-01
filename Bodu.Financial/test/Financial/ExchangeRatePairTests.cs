// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePairTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

[TestClass]
public partial class ExchangeRatePairTests
{
    /// <summary>
    /// Verifies that the constructor stores both ISO codes verbatim.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Constructor_WhenInputsValid_ShouldStoreBothCodes()
    {
        ExchangeRatePair pair = new("USD", "AUD");

        Assert.AreEqual("USD", pair.FromIsoCode);
        Assert.AreEqual("AUD", pair.ToIsoCode);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRatePair.Inverse" /> swaps the source and destination codes.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenCalled_ShouldReturnPairWithCodesSwapped()
    {
        ExchangeRatePair pair = new("USD", "AUD");

        ExchangeRatePair inverse = pair.Inverse();

        Assert.AreEqual("AUD", inverse.FromIsoCode);
        Assert.AreEqual("USD", inverse.ToIsoCode);
    }

    /// <summary>
    /// Verifies that two pairs with identical codes compare equal via the generated record-struct equality.
    /// </summary>
    [TestMethod]
    public void Equality_WhenComponentsMatch_ShouldReportEqual()
    {
        ExchangeRatePair a = new("USD", "AUD");
        ExchangeRatePair b = new("USD", "AUD");

        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that two pairs whose components differ in direction compare unequal.
    /// </summary>
    [TestMethod]
    public void Equality_WhenDirectionDiffers_ShouldReportUnequal()
    {
        ExchangeRatePair a = new("USD", "AUD");
        ExchangeRatePair b = new("AUD", "USD");

        Assert.AreNotEqual(a, b);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> source code throws <see cref="ArgumentNullException" /> with the
    /// parameter name <c>fromIsoCode</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenFromIsoCodeIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new ExchangeRatePair(null!, "AUD");
            },
            "fromIsoCode");
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> destination code throws <see cref="ArgumentNullException" /> with the
    /// parameter name <c>toIsoCode</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenToIsoCodeIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new ExchangeRatePair("USD", null!);
            },
            "toIsoCode");
    }

    /// <summary>
    /// Verifies that malformed ISO codes throw <see cref="ArgumentException" /> on either parameter.
    /// </summary>
    [TestMethod]
    [DataRow("usd", "AUD", "fromIsoCode")]
    [DataRow("US", "AUD", "fromIsoCode")]
    [DataRow("USDX", "AUD", "fromIsoCode")]
    [DataRow("USD", "aud", "toIsoCode")]
    [DataRow("USD", "AU", "toIsoCode")]
    public void Constructor_WhenCodeMalformed_ShouldThrowArgumentExceptionWithExpectedParamName(
        string from,
        string to,
        string expectedParamName)
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new ExchangeRatePair(from, to);
            },
            expectedParamName);
    }
}
