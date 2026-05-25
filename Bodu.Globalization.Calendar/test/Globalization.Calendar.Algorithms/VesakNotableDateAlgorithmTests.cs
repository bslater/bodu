// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VesakNotableDateAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the Vesak-specific behaviour of <see cref="VesakNotableDateAlgorithm" />: known-answer agreement
/// with the Thai Visakha Bucha public holiday and the supported-range property that every computed date
/// falls in May or early June, on or after 1 May.
/// </summary>
/// <remarks>
/// The shared boundary contract (year &lt; 1, year &gt; 9999, null/Gregorian/Julian calendar) is exercised
/// from <see cref="NotableDateAlgorithmContractTests" /> via the
/// <see cref="NotableDateAlgorithmKnownAnswers.AllAlgorithmFactories" /> enrollment.
/// </remarks>
[TestClass]
public sealed class VesakNotableDateAlgorithmTests
{
    private readonly VesakNotableDateAlgorithm _algorithm = new();

    /// <summary>
    /// Verifies that Vesak returns the published Thai Visakha Bucha date for the smoke-tier representative
    /// year. Runs on every BVT build.
    /// </summary>
    /// <param name="knownAnswer">The Vesak known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.VesakSmoke" />.</param>
    [DataTestMethod]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.VesakSmoke),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenGivenSmokeRows_ShouldReturnExpectedDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that Vesak returns the published Thai Visakha Bucha date for every row in the full known-
    /// answer table. Runs only under the Regression tier.
    /// </summary>
    /// <param name="knownAnswer">The Vesak known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.Vesak" />.</param>
    [DataTestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.Vesak),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenGivenAllKnownRows_ShouldReturnExpectedDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that for every year in the range 1901-2100 the result falls in May or June on or after 1
    /// May, consistent with the Visakha Bucha definition of the first full moon on or after 1 May.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenIteratingSupportedRange_ShouldAlwaysFallInMayOrEarlyJune()
    {
        for (int year = 1901; year <= 2100; year++)
        {
            DateTime? result = _algorithm.GetDate(year);

            Assert.IsNotNull(result, $"GetDate returned null for year {year}.");
            Assert.IsTrue(result!.Value.Month is 5 or 6,
                $"Expected May or June for year {year}, got month {result.Value.Month}.");
            Assert.IsTrue(result.Value >= new DateTime(year, 5, 1),
                $"Expected date on or after May 1 for year {year}, got {result.Value:yyyy-MM-dd}.");
        }
    }
}
