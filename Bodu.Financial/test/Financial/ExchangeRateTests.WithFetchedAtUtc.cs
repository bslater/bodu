// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateTests.WithFetchedAtUtc.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class ExchangeRateTests
{

    /// <summary>
    /// Verifies that <see cref="ExchangeRate.WithFetchedAtUtc" /> returns a copy carrying the supplied fetch instant.
    /// </summary>
    [TestMethod]
    public void WithFetchedAtUtc_WhenInstantSupplied_ShouldSetFetchedAtUtc()
    {
        var fetchedAt = new DateTimeOffset(2024, 1, 3, 9, 30, 0, TimeSpan.Zero);
        ExchangeRate original = new(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, 1.5m, "RBA");

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
        ExchangeRate original = new(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, 1.5m, "RBA", isInverted: false, new DateTimeOffset(2024, 1, 3, 9, 30, 0, TimeSpan.Zero));

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
        ExchangeRate original = new(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, 1.5m, "RBA");

        ExchangeRate copy = original.WithFetchedAtUtc(new DateTimeOffset(2024, 1, 3, 9, 30, 0, TimeSpan.Zero));

        Assert.AreEqual(original.From, copy.From);
        Assert.AreEqual(original.To, copy.To);
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
        var inverted = ExchangeRate.FromObservedRate(CurrencyCode.AUD, CurrencyCode.USD, s_sampleDate, 3m, "RBA", isInverted: true);

        ExchangeRate copy = inverted.WithFetchedAtUtc(new DateTimeOffset(2024, 1, 3, 9, 30, 0, TimeSpan.Zero));

        Assert.IsTrue(copy.IsInverted);
        Assert.AreEqual(inverted.Convert(99m), copy.Convert(99m));
    }
}
