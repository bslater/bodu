// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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

    /// <summary>
    /// Verifies that two exchange rates with identical fields produce the same hash code.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenFieldsMatch_ShouldBeEqual()
    {
        var a = new ExchangeRate("USD", "AUD", s_sampleDate, 1.5m, "ecb");
        var b = new ExchangeRate("USD", "AUD", s_sampleDate, 1.5m, "ecb");

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that a supplied fetch instant is exposed through <see cref="ExchangeRate.FetchedAtUtc" />.
    /// </summary>
    [TestMethod]
    public void FetchedAtUtc_WhenSupplied_ShouldBeExposed()
    {
        DateTimeOffset fetchedAt = new(2024, 1, 3, 9, 30, 0, TimeSpan.Zero);
        ExchangeRate rate = new("USD", "AUD", s_sampleDate, 1.5m, "RBA", isInverted: false, fetchedAt);

        Assert.AreEqual(fetchedAt, rate.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRate.FetchedAtUtc" /> is <see langword="null" /> when no fetch instant is
    /// supplied at construction.
    /// </summary>
    [TestMethod]
    public void FetchedAtUtc_WhenOmitted_ShouldBeNull()
    {
        ExchangeRate rate = new("USD", "AUD", s_sampleDate, 1.5m, "RBA");

        Assert.IsNull(rate.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that two exchange rates that differ only in their fetch instant compare equal, because the fetch
    /// instant is provenance metadata excluded from equality.
    /// </summary>
    [TestMethod]
    public void Equals_WhenOnlyFetchedAtUtcDiffers_ShouldBeEqual()
    {
        ExchangeRate stamped = new("USD", "AUD", s_sampleDate, 1.5m, "RBA", isInverted: false, new DateTimeOffset(2024, 1, 3, 9, 30, 0, TimeSpan.Zero));
        ExchangeRate other = new("USD", "AUD", s_sampleDate, 1.5m, "RBA", isInverted: false, new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));
        ExchangeRate unstamped = new("USD", "AUD", s_sampleDate, 1.5m, "RBA");

        Assert.AreEqual(stamped, other);
        Assert.AreEqual(stamped, unstamped);
    }

    /// <summary>
    /// Verifies that two exchange rates that differ only in their fetch instant produce the same hash code, because the
    /// fetch instant is excluded from the hash.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenOnlyFetchedAtUtcDiffers_ShouldBeEqual()
    {
        ExchangeRate stamped = new("USD", "AUD", s_sampleDate, 1.5m, "RBA", isInverted: false, new DateTimeOffset(2024, 1, 3, 9, 30, 0, TimeSpan.Zero));
        ExchangeRate unstamped = new("USD", "AUD", s_sampleDate, 1.5m, "RBA");

        Assert.AreEqual(unstamped.GetHashCode(), stamped.GetHashCode());
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRate.WithFetchedAtUtc" /> returns a copy carrying the supplied fetch instant.
    /// </summary>
    [TestMethod]
    public void WithFetchedAtUtc_WhenInstantSupplied_ShouldSetFetchedAtUtc()
    {
        var fetchedAt = new DateTimeOffset(2024, 1, 3, 9, 30, 0, TimeSpan.Zero);
        ExchangeRate original = new("USD", "AUD", s_sampleDate, 1.5m, "RBA");

        ExchangeRate copy = original.WithFetchedAtUtc(fetchedAt);

        Assert.AreEqual(fetchedAt, copy.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRate.WithFetchedAtUtc" /> clears the fetch instant when supplied
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithFetchedAtUtc_WhenNull_ShouldClearFetchedAtUtc()
    {
        ExchangeRate original = new("USD", "AUD", s_sampleDate, 1.5m, "RBA", isInverted: false, new DateTimeOffset(2024, 1, 3, 9, 30, 0, TimeSpan.Zero));

        ExchangeRate copy = original.WithFetchedAtUtc(null);

        Assert.IsNull(copy.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRate.WithFetchedAtUtc" /> preserves every other field, so the copy compares
    /// equal to the original (the fetch instant being excluded from equality).
    /// </summary>
    [TestMethod]
    public void WithFetchedAtUtc_WhenApplied_ShouldPreserveAllOtherFieldsAndCompareEqual()
    {
        ExchangeRate original = new("USD", "AUD", s_sampleDate, 1.5m, "RBA");

        ExchangeRate copy = original.WithFetchedAtUtc(new DateTimeOffset(2024, 1, 3, 9, 30, 0, TimeSpan.Zero));

        Assert.AreEqual(original.FromIsoCode, copy.FromIsoCode);
        Assert.AreEqual(original.ToIsoCode, copy.ToIsoCode);
        Assert.AreEqual(original.Date, copy.Date);
        Assert.AreEqual(original.Rate, copy.Rate);
        Assert.AreEqual(original.Provider, copy.Provider);
        Assert.AreEqual(original.IsInverted, copy.IsInverted);
        Assert.AreEqual(original, copy);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRate.WithFetchedAtUtc" /> preserves the precise reverse-pair conversion of an
    /// inverted rate, confirming the internal observed rate is carried over exactly rather than recomputed from the
    /// rounded public multiplier.
    /// </summary>
    [TestMethod]
    public void WithFetchedAtUtc_WhenInverted_ShouldPreservePreciseConversion()
    {
        // An inverted rate divides by the original reverse-pair rate; 1/3 is not exactly representable, so a recompute
        // from the rounded public multiplier would drift. The copy must convert identically to the original.
        ExchangeRate inverted = ExchangeRate.FromObservedRate("AUD", "USD", s_sampleDate, 3m, "RBA", isInverted: true);

        ExchangeRate copy = inverted.WithFetchedAtUtc(new DateTimeOffset(2024, 1, 3, 9, 30, 0, TimeSpan.Zero));

        Assert.IsTrue(copy.IsInverted);
        Assert.AreEqual(inverted.Convert(99m), copy.Convert(99m));
    }
}
