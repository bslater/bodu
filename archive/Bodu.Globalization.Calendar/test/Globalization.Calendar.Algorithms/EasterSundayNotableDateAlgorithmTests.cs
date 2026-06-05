// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EasterSundayNotableDateAlgorithmTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlob = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the Easter-Sunday-specific behaviour of <see cref="EasterSundayNotableDateAlgorithm" />: the
/// Julian / Gregorian computus branch selection at the 1583 cutover, explicit-Julian projection through the
/// calendar argument, per-year result caching, and exact known-answer agreement across the published almanac
/// dataset (years 1700-2093).
/// </summary>
/// <remarks>
/// <para>
/// The shared boundary contract (year &lt; 1, year &gt; 9999, null/Gregorian/Julian calendar) is exercised
/// from <see cref="NotableDateAlgorithmContractTests" /> via the
/// <see cref="NotableDateAlgorithmKnownAnswers.AllAlgorithmFactories" /> enrollment.
/// </para>
/// <para>
/// Orthodox rows (years 1916-2099) live in
/// <see cref="NotableDateAlgorithmKnownAnswers.EasterOrthodox" /> and run under the Regression tier.
/// Previously gated by <c>[Ignore]</c> per
/// <see href="https://github.com/bslater/bodu/issues/53" />; the underlying algorithm bug was resolved
/// and the gate has been removed.
/// </para>
/// </remarks>
[TestClass]
public sealed class EasterSundayNotableDateAlgorithmTests
{
    private readonly EasterSundayNotableDateAlgorithm _algorithm = new();

    /// <summary>
    /// Verifies that Easter Sunday is correctly calculated for the smoke-tier representative years against
    /// the default (Gregorian) calendar.
    /// </summary>
    /// <param name="knownAnswer">The Easter known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.EasterSmoke" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.EasterSmoke),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenGivenSmokeRows_ShouldReturnExpectedDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that Easter Sunday is correctly calculated for every row in the full default-calendar table
    /// (376 rows spanning years 1700-2093). Runs only under the Regression tier.
    /// </summary>
    /// <param name="knownAnswer">The Easter known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.EasterDefault" />.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.EasterDefault),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenGivenAllKnownRows_ShouldReturnExpectedDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that Orthodox Easter Sunday is correctly calculated for every year in 1916-2099. The
    /// algorithm computes the Julian paschal date via Meeus's formula and renders it as a Gregorian
    /// <see cref="DateTime" /> through <see cref="System.Globalization.JulianCalendar.ToDateTime(int, int, int, int, int, int, int)" />.
    /// </summary>
    /// <param name="knownAnswer">The Orthodox Easter known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.EasterOrthodox" />.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.EasterOrthodox),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenGivenOrthodoxKnownYears_ShouldReturnExpectedDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that Julian-era Easter Sundays (year &lt; 1583) resolve using the Julian computus and that
    /// their <see cref="DateTime" /> values match the expected almanac dates when no calendar is supplied.
    /// </summary>
    /// <param name="year">The year passed to the algorithm.</param>
    /// <param name="expectedY">The expected result year.</param>
    /// <param name="expectedM">The expected result month.</param>
    /// <param name="expectedD">The expected result day.</param>
    [DataRow(500, 500, 4, 2)]
    [DataRow(1000, 1000, 3, 31)]
    [DataRow(1500, 1500, 4, 19)]
    [DataRow(1582, 1582, 4, 15)]
    [TestMethod]
    public void GetDate_WhenJulianEraYear_ShouldComputeUsingJulianBranch(int year, int expectedY, int expectedM, int expectedD)
    {
        DateTime? result = _algorithm.GetDate(year, null);

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateTime(expectedY, expectedM, expectedD), result);
    }

    /// <summary>
    /// Verifies that the Gregorian vs. Julian computation branches are selected based on the 1583 cutover
    /// year — 1583 is the Gregorian computus's minimum supported year, so both 1583 and 1584 take that
    /// branch.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenAtOrAbove1583_ShouldUseGregorianBranch()
    {
        DateTime? gregorian1583 = _algorithm.GetDate(1583, null);
        DateTime? gregorian1584 = _algorithm.GetDate(1584, null);

        Assert.IsNotNull(gregorian1583);
        Assert.IsNotNull(gregorian1584);
        Assert.AreEqual(1583, gregorian1583!.Value.Year);
        Assert.AreEqual(1584, gregorian1584!.Value.Year);
    }

    /// <summary>
    /// Verifies that passing a Julian calendar explicitly yields Julian-calendar <see cref="DateTime" />
    /// values with <see cref="DateTimeKind.Unspecified" /> kind rather than the raw Gregorian output. Covers
    /// the non-null <c>calendar.ToDateTime</c> branch in the algorithm.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenExplicitJulianCalendar_ShouldReturnDateBuiltThroughCalendar()
    {
        SysGlob.JulianCalendar julian = new();

        DateTime? result = _algorithm.GetDate(2026, julian);

        Assert.IsNotNull(result);
        Assert.AreEqual(DateTimeKind.Unspecified, result!.Value.Kind);
    }

    /// <summary>
    /// Verifies that repeated calls to <see cref="EasterSundayNotableDateAlgorithm.GetDate" /> for the same
    /// year and calendar return the same cached result.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenCalledTwice_ShouldReturnCachedResult()
    {
        var year = 2026;

        DateTime? firstCall = _algorithm.GetDate(year, null);
        DateTime? secondCall = _algorithm.GetDate(year, null);

        Assert.AreEqual(firstCall, secondCall);
    }
}
