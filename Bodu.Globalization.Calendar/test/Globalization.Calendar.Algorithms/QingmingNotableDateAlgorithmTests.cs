// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QingmingNotableDateAlgorithmTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the Qingming-specific behaviour of <see cref="QingmingNotableDateAlgorithm" />: known-answer
/// agreement (within a one-day tolerance) with published astronomical dates, and the supported-range
/// property that every computed date falls between 3 and 6 April inclusive.
/// </summary>
/// <remarks>
/// The shared boundary contract (year &lt; 1, year &gt; 9999, null/Gregorian/Julian calendar) is exercised
/// from <see cref="NotableDateAlgorithmContractTests" /> via the
/// <see cref="NotableDateAlgorithmKnownAnswers.AllAlgorithmFactories" /> enrollment.
/// </remarks>
[TestClass]
public sealed class QingmingNotableDateAlgorithmTests
{
    private readonly QingmingNotableDateAlgorithm _algorithm = new();

    /// <summary>
    /// Verifies that Qingming falls within one calendar day of the published astronomical date for the
    /// smoke-tier representative year. Runs on every BVT build.
    /// </summary>
    /// <param name="knownAnswer">The Qingming known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.QingmingSmoke" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.QingmingSmoke),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenGivenSmokeRows_ShouldFallWithinOneDayOfExpectedDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that Qingming falls within one calendar day of the published astronomical date for every row
    /// in the full known-answer table. The algorithm computes solar-term transitions in Universal Time;
    /// published dates in East Asia use CST (UTC+8) and may differ by one calendar day when the transition
    /// falls near local midnight. Runs only under the Regression tier.
    /// </summary>
    /// <param name="knownAnswer">The Qingming known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.Qingming" />.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.Qingming),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenGivenAllKnownRows_ShouldFallWithinOneDayOfExpectedDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that for every year in the range 1901-2100 the result falls between 3 and 6 April inclusive.
    /// Qingming typically falls on 4 or 5 April; the bounds of 3 and 6 account for the algorithm's stated
    /// +/- 1 day accuracy and the long-term secular drift of the solar year across the full range.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenIteratingSupportedRange_ShouldAlwaysFallBetweenApril3And6()
    {
        for (var year = 1901; year <= 2100; year++)
        {
            DateTime? result = _algorithm.GetDate(year);

            Assert.IsNotNull(result, $"GetDate returned null for year {year}.");
            Assert.AreEqual(4, result!.Value.Month, $"Expected April for year {year}.");
            Assert.IsTrue(result.Value.Day is >= 3 and <= 6,
                $"Expected day 3-6 for year {year}, got {result.Value.Day}.");
        }
    }
}
