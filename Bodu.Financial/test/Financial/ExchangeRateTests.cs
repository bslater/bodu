// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

[TestClass]
public partial class ExchangeRateTests
{
    private static readonly DateOnly s_sampleDate = new(2024, 1, 3);

    /// <summary>
    /// Verifies that constructing an <see cref="ExchangeRate" /> with valid inputs stores every component verbatim.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Constructor_WhenAllInputsValid_ShouldPopulateEveryComponent()
    {
        ExchangeRate rate = new("USD", "AUD", s_sampleDate, 1.5m, "RBA");

        Assert.AreEqual("USD", rate.FromIsoCode);
        Assert.AreEqual("AUD", rate.ToIsoCode);
        Assert.AreEqual(s_sampleDate, rate.Date);
        Assert.AreEqual(1.5m, rate.Rate);
        Assert.AreEqual("RBA", rate.Provider);
        Assert.IsFalse(rate.IsInverted);
    }

    /// <summary>
    /// Verifies that the <see cref="ExchangeRate.IsInverted" /> flag is propagated as supplied.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenIsInvertedSpecified_ShouldStoreFlag()
    {
        ExchangeRate rate = new("AUD", "USD", s_sampleDate, 0.6m, "RBA", isInverted: true);

        Assert.IsTrue(rate.IsInverted);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRate.Convert(decimal)" /> multiplies the input by the stored rate.
    /// </summary>
    [TestMethod]
    public void Convert_WhenCalled_ShouldReturnAmountMultipliedByRate()
    {
        ExchangeRate rate = new("USD", "AUD", s_sampleDate, 1.5m, "RBA");

        var converted = rate.Convert(100m);

        Assert.AreEqual(150m, converted);
    }

    /// <summary>
    /// Verifies that constructing an <see cref="ExchangeRate" /> with a <see langword="null" /> source-currency code
    /// throws <see cref="ArgumentNullException" /> with the parameter name <c>fromIsoCode</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenFromIsoCodeIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new ExchangeRate(null!, "AUD", s_sampleDate, 1m, "RBA");
            },
            "fromIsoCode");
    }

    /// <summary>
    /// Verifies that an invalid source-currency code throws <see cref="ArgumentException" /> with the parameter name
    /// <c>fromIsoCode</c>.
    /// </summary>
    [TestMethod]
    [DataRow("usd")]
    [DataRow("US")]
    [DataRow("USDX")]
    [DataRow("   ")]
    [DataRow("US1")]
    public void Constructor_WhenFromIsoCodeIsMalformed_ShouldThrowArgumentException(string value)
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new ExchangeRate(value, "AUD", s_sampleDate, 1m, "RBA");
            },
            "fromIsoCode");
    }

    /// <summary>
    /// Verifies that an invalid destination-currency code throws <see cref="ArgumentException" /> with the parameter
    /// name <c>toIsoCode</c>.
    /// </summary>
    [TestMethod]
    [DataRow("aud")]
    [DataRow("AU")]
    [DataRow("AUDX")]
    public void Constructor_WhenToIsoCodeIsMalformed_ShouldThrowArgumentException(string value)
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new ExchangeRate("USD", value, s_sampleDate, 1m, "RBA");
            },
            "toIsoCode");
    }

    /// <summary>
    /// Verifies that a zero or negative rate throws <see cref="ArgumentOutOfRangeException" /> with the parameter name
    /// <c>rate</c>.
    /// </summary>
    [TestMethod]
    [DataRow("0")]
    [DataRow("-0.01")]
    [DataRow("-100")]
    public void Constructor_WhenRateIsNotPositive_ShouldThrowArgumentOutOfRangeException(string value)
    {
        var rate = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new ExchangeRate("USD", "AUD", s_sampleDate, rate, "RBA");
            },
            "rate");
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> provider throws <see cref="ArgumentNullException" /> with the parameter
    /// name <c>provider</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new ExchangeRate("USD", "AUD", s_sampleDate, 1m, null!);
            },
            "provider");
    }

    /// <summary>
    /// Verifies that an empty or whitespace provider throws <see cref="ArgumentException" /> with the parameter name
    /// <c>provider</c>.
    /// </summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("\t")]
    public void Constructor_WhenProviderIsEmptyOrWhitespace_ShouldThrowArgumentException(string value)
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new ExchangeRate("USD", "AUD", s_sampleDate, 1m, value);
            },
            "provider");
    }
}
