// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesTests.TryGetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Kat;
using Bodu.Test.Kat;

namespace Bodu.Financial;

public partial class ExchangeRateSeriesTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeries.TryGetRate" /> resolves the expected date under every documented
    /// <see cref="ExchangeRateDateResolution" /> policy and tolerance combination.
    /// </summary>
    [TestMethod]
    [DynamicData(
        nameof(DateResolutionCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryGetRate_WhenDateResolutionPolicyApplied_ShouldMatchExpectedOutcome(ExchangeRateDateResolutionKat kat)
    {
        (DateOnly Date, decimal Rate)[] rates = [.. kat.AvailableDates.Select((date, index) => (date, 1m + (index * 0.01m)))];

        ExchangeRateSeries series = new(new ExchangeRatePair("USD", "AUD"), "RBA", rates);
        ExchangeRateLookupOptions options = new(kat.Resolution, kat.ToleranceDays);

        var actual = series.TryGetRate(kat.RequestedDate, options, out DateOnly resolved, out _);

        Assert.AreEqual(kat.ExpectedSuccess, actual);

        if (kat.ExpectedSuccess)
            Assert.AreEqual(kat.ExpectedDate, resolved);
    }

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

        yield return [new ExchangeRateDateResolutionKat(
            "Exact hit",
            new DateOnly(2024, 1, 3),
            ExchangeRateDateResolution.Exact,
            0,
            basic,
            new DateOnly(2024, 1, 3),
            true)];

        yield return [new ExchangeRateDateResolutionKat(
            "Exact miss",
            new DateOnly(2024, 1, 4),
            ExchangeRateDateResolution.Exact,
            0,
            basic,
            null,
            false)];

        yield return [new ExchangeRateDateResolutionKat(
            "Previous within tolerance",
            new DateOnly(2024, 1, 4),
            ExchangeRateDateResolution.PreviousOnOrBefore,
            2,
            basic,
            new DateOnly(2024, 1, 3),
            true)];

        yield return [new ExchangeRateDateResolutionKat(
            "Previous outside tolerance",
            new DateOnly(2024, 1, 7),
            ExchangeRateDateResolution.PreviousOnOrBefore,
            1,
            basic,
            null,
            false)];

        yield return [new ExchangeRateDateResolutionKat(
            "Previous before first available",
            new DateOnly(2024, 1, 1),
            ExchangeRateDateResolution.PreviousOnOrBefore,
            30,
            basic,
            null,
            false)];

        yield return [new ExchangeRateDateResolutionKat(
            "Next within tolerance",
            new DateOnly(2024, 1, 4),
            ExchangeRateDateResolution.NextOnOrAfter,
            2,
            basic,
            new DateOnly(2024, 1, 5),
            true)];

        yield return [new ExchangeRateDateResolutionKat(
            "Next outside tolerance",
            new DateOnly(2024, 1, 1),
            ExchangeRateDateResolution.NextOnOrAfter,
            0,
            basic,
            null,
            false)];

        yield return [new ExchangeRateDateResolutionKat(
            "Next after last available",
            new DateOnly(2024, 1, 10),
            ExchangeRateDateResolution.NextOnOrAfter,
            30,
            basic,
            null,
            false)];

        yield return [new ExchangeRateDateResolutionKat(
            "Nearest chooses previous (closer)",
            new DateOnly(2024, 1, 4),
            ExchangeRateDateResolution.Nearest,
            2,
            gap,
            new DateOnly(2024, 1, 2),
            true)];

        yield return [new ExchangeRateDateResolutionKat(
            "Nearest chooses next (closer)",
            new DateOnly(2024, 1, 7),
            ExchangeRateDateResolution.Nearest,
            2,
            gap,
            new DateOnly(2024, 1, 8),
            true)];

        yield return [new ExchangeRateDateResolutionKat(
            "Nearest tie rejected",
            new DateOnly(2024, 1, 5),
            ExchangeRateDateResolution.Nearest,
            10,
            gap,
            null,
            false)];

        yield return [new ExchangeRateDateResolutionKat(
            "NearestPreferPrevious tie chooses previous",
            new DateOnly(2024, 1, 5),
            ExchangeRateDateResolution.NearestPreferPrevious,
            10,
            gap,
            new DateOnly(2024, 1, 2),
            true)];

        yield return [new ExchangeRateDateResolutionKat(
            "NearestPreferNext tie chooses next",
            new DateOnly(2024, 1, 5),
            ExchangeRateDateResolution.NearestPreferNext,
            10,
            gap,
            new DateOnly(2024, 1, 8),
            true)];

        yield return [new ExchangeRateDateResolutionKat(
            "Nearest exact match preferred",
            new DateOnly(2024, 1, 3),
            ExchangeRateDateResolution.Nearest,
            0,
            basic,
            new DateOnly(2024, 1, 3),
            true)];

        yield return [new ExchangeRateDateResolutionKat(
            "Nearest single-date series before",
            new DateOnly(2024, 1, 1),
            ExchangeRateDateResolution.Nearest,
            10,
            [new DateOnly(2024, 1, 5)],
            new DateOnly(2024, 1, 5),
            true)];

        yield return [new ExchangeRateDateResolutionKat(
            "Nearest single-date series after",
            new DateOnly(2024, 1, 10),
            ExchangeRateDateResolution.Nearest,
            10,
            [new DateOnly(2024, 1, 5)],
            new DateOnly(2024, 1, 5),
            true)];

        yield return [new ExchangeRateDateResolutionKat(
            "Nearest outside tolerance",
            new DateOnly(2024, 1, 20),
            ExchangeRateDateResolution.Nearest,
            3,
            basic,
            null,
            false)];

        yield return [new ExchangeRateDateResolutionKat(
            "Exact at DateOnly.MinValue",
            DateOnly.MinValue,
            ExchangeRateDateResolution.Exact,
            0,
            [DateOnly.MinValue, new DateOnly(2024, 1, 5)],
            DateOnly.MinValue,
            true)];

        yield return [new ExchangeRateDateResolutionKat(
            "Exact at DateOnly.MaxValue",
            DateOnly.MaxValue,
            ExchangeRateDateResolution.Exact,
            0,
            [new DateOnly(2024, 1, 5), DateOnly.MaxValue],
            DateOnly.MaxValue,
            true)];

        yield return [new ExchangeRateDateResolutionKat(
            "Leap day resolved exact",
            new DateOnly(2024, 2, 29),
            ExchangeRateDateResolution.Exact,
            0,
            [new DateOnly(2024, 2, 28), new DateOnly(2024, 2, 29), new DateOnly(2024, 3, 1)],
            new DateOnly(2024, 2, 29),
            true)];
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeries.TryGetRate" /> throws when supplied options carry an undefined enum value.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenOptionsContainUndefinedEnum_ShouldThrowArgumentOutOfRangeException()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", SampleRates());

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ExchangeRateLookupOptions bad = new((ExchangeRateDateResolution)999);
            series.TryGetRate(new DateOnly(2024, 1, 3), bad, out _, out _);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeries.TryGetRate" /> throws when <c>Exact</c> is paired with non-zero tolerance.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenExactCombinedWithNonZeroTolerance_ShouldThrowArgumentException()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", SampleRates());

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ExchangeRateLookupOptions bad = new(ExchangeRateDateResolution.Exact, 1);
            series.TryGetRate(new DateOnly(2024, 1, 3), bad, out _, out _);
        });
    }

    /// <summary>
    /// Verifies that an exact-hit lookup returns the rate aligned positionally with the matched date.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenExactHit_ShouldReturnAlignedRate()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", SampleRates());

        var found = series.TryGetRate(
            new DateOnly(2024, 1, 5),
            ExchangeRateLookupOptions.Exact,
            out DateOnly resolved,
            out var rate);

        Assert.IsTrue(found);
        Assert.AreEqual(new DateOnly(2024, 1, 5), resolved);
        Assert.AreEqual(1.52m, rate);
    }
}
