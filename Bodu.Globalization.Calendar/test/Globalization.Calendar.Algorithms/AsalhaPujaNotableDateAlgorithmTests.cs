// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsalhaPujaNotableDateAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the Asalha-Puja-specific behaviour of <see cref="AsalhaPujaNotableDateAlgorithm" />: known-
/// answer agreement with the Thai Asanha Bucha public holiday and the supported-range property that every
/// computed date falls in June or July, on or after 15 June.
/// </summary>
/// <remarks>
/// The shared boundary contract (year &lt; 1, year &gt; 9999, null/Gregorian/Julian calendar) is exercised
/// from <see cref="NotableDateAlgorithmContractTests" /> via the
/// <see cref="NotableDateAlgorithmKnownAnswers.AllAlgorithmFactories" /> enrollment.
/// </remarks>
[TestClass]
public sealed class AsalhaPujaNotableDateAlgorithmTests
{
    private readonly AsalhaPujaNotableDateAlgorithm _algorithm = new();

    /// <summary>
    /// Verifies that Asalha Puja returns the published Thai Asanha Bucha date for the smoke-tier
    /// representative year. Runs on every BVT build.
    /// </summary>
    /// <param name="knownAnswer">The Asalha Puja known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.AsalhaPujaSmoke" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.AsalhaPujaSmoke),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenGivenSmokeRows_ShouldReturnExpectedDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that Asalha Puja returns the published Thai Asanha Bucha date for every row in the full
    /// known-answer table — years with Thai intercalary months are excluded because the official observance
    /// is moved to the following lunation. Runs only under the Regression tier.
    /// </summary>
    /// <param name="knownAnswer">The Asalha Puja known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.AsalhaPuja" />.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.AsalhaPuja),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenGivenAllKnownRows_ShouldReturnExpectedDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that for every year in the range 1901-2100 the result falls in June or July on or after 15
    /// June, consistent with the algorithm's definition of the first full moon on or after that search
    /// start.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenIteratingSupportedRange_ShouldAlwaysFallInJuneOrJuly()
    {
        for (int year = 1901; year <= 2100; year++)
        {
            DateTime? result = _algorithm.GetDate(year);

            Assert.IsNotNull(result, $"GetDate returned null for year {year}.");
            Assert.IsTrue(result!.Value.Month is 6 or 7,
                $"Expected June or July for year {year}, got month {result.Value.Month}.");
            Assert.IsTrue(result.Value >= new DateTime(year, 6, 15),
                $"Expected date on or after 15 June for year {year}, got {result.Value:yyyy-MM-dd}.");
        }
    }
}
