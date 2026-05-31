// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LosarNotableDateAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the Losar-specific behaviour of <see cref="LosarNotableDateAlgorithm" />: known-answer agreement
/// with the official Tibetan New Year (within a one-day tolerance) and the supported-range property that
/// every computed date falls in January or February on or after 20 January.
/// </summary>
/// <remarks>
/// <para>
/// The shared boundary contract (year &lt; 1, year &gt; 9999, null/Gregorian/Julian calendar) is exercised
/// from <see cref="NotableDateAlgorithmContractTests" /> via the
/// <see cref="NotableDateAlgorithmKnownAnswers.AllAlgorithmFactories" /> enrollment.
/// </para>
/// <para>
/// The Losar algorithm returns the first new moon on or after 20 January as an approximation of the Tibetan
/// New Year. In years where the Tibetan lunisolar calendar inserts an intercalary month before the first
/// month, the algorithm may return a date approximately one month earlier than the actual Losar. Known-date
/// rows therefore select years where the algorithm is known to agree with the official Tibetan calendar.
/// </para>
/// </remarks>
[TestClass]
public sealed class LosarNotableDateAlgorithmTests
{
    private readonly LosarNotableDateAlgorithm _algorithm = new();

    /// <summary>
    /// Verifies that Losar returns a date within +/- one day of the known official Tibetan New Year for the
    /// smoke-tier representative year. Runs on every BVT build.
    /// </summary>
    /// <param name="knownAnswer">The Losar known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.LosarSmoke" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.LosarSmoke),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenGivenSmokeRows_ShouldReturnExpectedDateWithinTolerance(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that Losar returns a date within +/- one day of the known official Tibetan New Year for
    /// every published row where the lunar approximation agrees with the Tibetan calendar. Runs only under
    /// the Regression tier.
    /// </summary>
    /// <param name="knownAnswer">The Losar known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.Losar" />.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.Losar),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenGivenAllKnownRows_ShouldReturnExpectedDateWithinTolerance(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that for every year in the range 1901-2100 the result falls in January or February on or
    /// after 20 January, consistent with the algorithm's definition of the first new moon on or after that
    /// search start. This is a structural property of the lunar-approximation, not a known-answer row, and
    /// therefore stays as a bespoke test.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenIteratingSupportedRange_ShouldAlwaysFallInJanuaryOrFebruary()
    {
        for (int year = 1901; year <= 2100; year++)
        {
            DateTime? result = _algorithm.GetDate(year);

            Assert.IsNotNull(result, $"GetDate returned null for year {year}.");
            Assert.IsTrue(result!.Value.Month is 1 or 2,
                $"Expected January or February for year {year}, got month {result.Value.Month}.");
            Assert.IsTrue(result.Value >= new DateTime(year, 1, 20),
                $"Expected date on or after 20 January for year {year}, got {result.Value:yyyy-MM-dd}.");
        }
    }
}
