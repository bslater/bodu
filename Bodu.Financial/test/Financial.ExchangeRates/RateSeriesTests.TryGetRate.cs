// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesTests.TryGetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Kat;
using Bodu.Test.Kat;

namespace Bodu.Financial.ExchangeRates;

public partial class RateSeriesTests
{
    /// <summary>
    /// Verifies that <see cref="RateSeries.TryGetRate" /> resolves the expected date under every documented
    /// <see cref="RateDateResolution" /> policy and tolerance combination that is expected to succeed.
    /// </summary>
    [TestMethod]
    [DynamicData(
        nameof(DateResolutionSuccessCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryGetRate_WhenResolutionSucceeds_ShouldReturnExpectedDate(RateDateResolutionKat kat)
    {
        (DateOnly Date, decimal Rate)[] rates = [.. kat.AvailableDates.Select((date, index) => (date, 1m + (index * 0.01m)))];

        RateSeries series = new(new CurrencyPair(CurrencyCode.USD, CurrencyCode.AUD), "RBA", rates);
        RateLookupOptions options = new(kat.Resolution, kat.ToleranceDays);

        bool actual = series.TryGetRate(kat.RequestedDate, options, out DateOnly resolved, out _);

        Assert.IsTrue(actual);
        Assert.AreEqual(kat.ExpectedDate, resolved);
    }

    /// <summary>
    /// Verifies that <see cref="RateSeries.TryGetRate" /> returns <see langword="false" /> under every documented
    /// <see cref="RateDateResolution" /> policy and tolerance combination that is expected to fail.
    /// </summary>
    [TestMethod]
    [DynamicData(
        nameof(DateResolutionFailureCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryGetRate_WhenResolutionFails_ShouldReturnFalse(RateDateResolutionKat kat)
    {
        (DateOnly Date, decimal Rate)[] rates = [.. kat.AvailableDates.Select((date, index) => (date, 1m + (index * 0.01m)))];

        RateSeries series = new(new CurrencyPair(CurrencyCode.USD, CurrencyCode.AUD), "RBA", rates);
        RateLookupOptions options = new(kat.Resolution, kat.ToleranceDays);

        bool actual = series.TryGetRate(kat.RequestedDate, options, out _, out _);

        Assert.IsFalse(actual);
    }

    public static IEnumerable<object[]> DateResolutionSuccessCases() =>
        DateResolutionCases().Where(row => ((RateDateResolutionKat)row[0]).ExpectedSuccess);

    public static IEnumerable<object[]> DateResolutionFailureCases() =>
        DateResolutionCases().Where(row => !((RateDateResolutionKat)row[0]).ExpectedSuccess);

    public static IEnumerable<object[]> DateResolutionCases()
    {
        DateOnly[] basic =
        [
            new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 3),
            new DateOnly(2024, 1, 5),
        ];

        DateOnly[] gap =
        [
            new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 8),
        ];

        yield return [new RateDateResolutionKat(
            "Exact hit",
            new DateOnly(2024, 1, 3),
            RateDateResolution.Exact,
            0,
            basic,
            new DateOnly(2024, 1, 3),
            true)];

        yield return [new RateDateResolutionKat(
            "Exact miss",
            new DateOnly(2024, 1, 4),
            RateDateResolution.Exact,
            0,
            basic,
            null,
            false)];

        yield return [new RateDateResolutionKat(
            "Previous within tolerance",
            new DateOnly(2024, 1, 4),
            RateDateResolution.PreviousOnOrBefore,
            2,
            basic,
            new DateOnly(2024, 1, 3),
            true)];

        yield return [new RateDateResolutionKat(
            "Previous outside tolerance",
            new DateOnly(2024, 1, 7),
            RateDateResolution.PreviousOnOrBefore,
            1,
            basic,
            null,
            false)];

        yield return [new RateDateResolutionKat(
            "Previous before first available",
            new DateOnly(2024, 1, 1),
            RateDateResolution.PreviousOnOrBefore,
            30,
            basic,
            null,
            false)];

        yield return [new RateDateResolutionKat(
            "Next within tolerance",
            new DateOnly(2024, 1, 4),
            RateDateResolution.NextOnOrAfter,
            2,
            basic,
            new DateOnly(2024, 1, 5),
            true)];

        yield return [new RateDateResolutionKat(
            "Next outside tolerance",
            new DateOnly(2024, 1, 1),
            RateDateResolution.NextOnOrAfter,
            0,
            basic,
            null,
            false)];

        yield return [new RateDateResolutionKat(
            "Next after last available",
            new DateOnly(2024, 1, 10),
            RateDateResolution.NextOnOrAfter,
            30,
            basic,
            null,
            false)];

        yield return [new RateDateResolutionKat(
            "Nearest chooses previous (closer)",
            new DateOnly(2024, 1, 4),
            RateDateResolution.Nearest,
            2,
            gap,
            new DateOnly(2024, 1, 2),
            true)];

        yield return [new RateDateResolutionKat(
            "Nearest chooses next (closer)",
            new DateOnly(2024, 1, 7),
            RateDateResolution.Nearest,
            2,
            gap,
            new DateOnly(2024, 1, 8),
            true)];

        yield return [new RateDateResolutionKat(
            "Nearest tie rejected",
            new DateOnly(2024, 1, 5),
            RateDateResolution.Nearest,
            10,
            gap,
            null,
            false)];

        yield return [new RateDateResolutionKat(
            "NearestPreferPrevious tie chooses previous",
            new DateOnly(2024, 1, 5),
            RateDateResolution.NearestPreferPrevious,
            10,
            gap,
            new DateOnly(2024, 1, 2),
            true)];

        yield return [new RateDateResolutionKat(
            "NearestPreferNext tie chooses next",
            new DateOnly(2024, 1, 5),
            RateDateResolution.NearestPreferNext,
            10,
            gap,
            new DateOnly(2024, 1, 8),
            true)];

        yield return [new RateDateResolutionKat(
            "Nearest exact match preferred",
            new DateOnly(2024, 1, 3),
            RateDateResolution.Nearest,
            0,
            basic,
            new DateOnly(2024, 1, 3),
            true)];

        yield return [new RateDateResolutionKat(
            "Nearest single-date series before",
            new DateOnly(2024, 1, 1),
            RateDateResolution.Nearest,
            10,
            [new DateOnly(2024, 1, 5)],
            new DateOnly(2024, 1, 5),
            true)];

        yield return [new RateDateResolutionKat(
            "Nearest single-date series after",
            new DateOnly(2024, 1, 10),
            RateDateResolution.Nearest,
            10,
            [new DateOnly(2024, 1, 5)],
            new DateOnly(2024, 1, 5),
            true)];

        yield return [new RateDateResolutionKat(
            "Nearest outside tolerance",
            new DateOnly(2024, 1, 20),
            RateDateResolution.Nearest,
            3,
            basic,
            null,
            false)];

        yield return [new RateDateResolutionKat(
            "Exact at DateOnly.MinValue",
            DateOnly.MinValue,
            RateDateResolution.Exact,
            0,
            [DateOnly.MinValue, new DateOnly(2024, 1, 5)],
            DateOnly.MinValue,
            true)];

        yield return [new RateDateResolutionKat(
            "Exact at DateOnly.MaxValue",
            DateOnly.MaxValue,
            RateDateResolution.Exact,
            0,
            [new DateOnly(2024, 1, 5), DateOnly.MaxValue],
            DateOnly.MaxValue,
            true)];

        yield return [new RateDateResolutionKat(
            "Leap day resolved exact",
            new DateOnly(2024, 2, 29),
            RateDateResolution.Exact,
            0,
            [new DateOnly(2024, 2, 28), new DateOnly(2024, 2, 29), new DateOnly(2024, 3, 1)],
            new DateOnly(2024, 2, 29),
            true)];
    }

    /// <summary>
    /// Verifies that <see cref="RateSeries.TryGetRate" /> throws when supplied options carry an undefined enum value.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenOptionsContainUndefinedEnum_ShouldThrowArgumentOutOfRangeException()
    {
        RateSeries series = new(s_usdAud, "RBA", SampleRates());

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            RateLookupOptions bad = new((RateDateResolution)999);
            series.TryGetRate(new DateOnly(2024, 1, 3), bad, out _, out _);
        });
    }

    /// <summary>
    /// Verifies that <see cref="RateSeries.TryGetRate" /> throws when <c>Exact</c> is paired with non-zero tolerance.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenExactCombinedWithNonZeroTolerance_ShouldThrowArgumentException()
    {
        RateSeries series = new(s_usdAud, "RBA", SampleRates());

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            RateLookupOptions bad = new(RateDateResolution.Exact, 1);
            series.TryGetRate(new DateOnly(2024, 1, 3), bad, out _, out _);
        });
    }

    /// <summary>
    /// Verifies that an exact-hit lookup returns the rate aligned positionally with the matched date.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenExactHit_ShouldReturnAlignedRate()
    {
        RateSeries series = new(s_usdAud, "RBA", SampleRates());

        bool found = series.TryGetRate(
            new DateOnly(2024, 1, 5),
            RateLookupOptions.Exact,
            out DateOnly resolved,
            out decimal rate);

        Assert.IsTrue(found);
        Assert.AreEqual(new DateOnly(2024, 1, 5), resolved);
        Assert.AreEqual(1.52m, rate);
    }
}
