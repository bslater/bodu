// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateTests.Constructor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateTests
{

    /// <summary>
    /// Verifies that constructing an <see cref="ExchangeRate" /> with valid inputs stores every component verbatim.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Constructor_WhenAllInputsValid_ShouldPopulateEveryComponent()
    {
        ExchangeRate rate = new(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, 1.5m, "RBA");

        Assert.AreEqual(CurrencyCode.USD, rate.From);
        Assert.AreEqual(CurrencyCode.AUD, rate.To);
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
        ExchangeRate rate = new(CurrencyCode.AUD, CurrencyCode.USD, s_sampleDate, 0.6m, "RBA", isInverted: true);

        Assert.IsTrue(rate.IsInverted);
    }

    /// <summary>
    /// Verifies that constructing an <see cref="ExchangeRate" /> with a <see cref="CurrencyCode.None" /> source currency
    /// throws <see cref="ArgumentOutOfRangeException" /> with the parameter name <c>from</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenFromIsNone_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new ExchangeRate(CurrencyCode.None, CurrencyCode.AUD, s_sampleDate, 1m, "RBA");
            },
            "from");
    }

    /// <summary>
    /// Verifies that an undefined source currency throws <see cref="ArgumentOutOfRangeException" /> with the parameter
    /// name <c>from</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenFromCodeUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new ExchangeRate((CurrencyCode)9999, CurrencyCode.AUD, s_sampleDate, 1m, "RBA");
            },
            "from");
    }

    /// <summary>
    /// Verifies that constructing an <see cref="ExchangeRate" /> with a <see cref="CurrencyCode.None" /> destination
    /// currency throws <see cref="ArgumentOutOfRangeException" /> with the parameter name <c>to</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenToIsNone_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new ExchangeRate(CurrencyCode.USD, CurrencyCode.None, s_sampleDate, 1m, "RBA");
            },
            "to");
    }

    /// <summary>
    /// Verifies that an undefined destination currency throws <see cref="ArgumentOutOfRangeException" /> with the
    /// parameter name <c>to</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenToCodeUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new ExchangeRate(CurrencyCode.USD, (CurrencyCode)9999, s_sampleDate, 1m, "RBA");
            },
            "to");
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
        decimal rate = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, rate, "RBA");
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
                _ = new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, 1m, null!);
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
                _ = new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, 1m, value);
            },
            "provider");
    }
}
