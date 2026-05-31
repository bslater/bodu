// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HinduLunarNotableDateAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the Hindu-lunar-specific behaviour of <see cref="HinduLunarNotableDateAlgorithm" />: constructor
/// validation, per-festival known-answer agreement (within a two-day tolerance) for Diwali, Holi, and
/// Navaratri, and the supported-range property that every defined <see cref="HinduLunarMonth" /> produces a
/// non-null date.
/// </summary>
/// <remarks>
/// <para>
/// Expected dates come from widely published Indian panchanga sources. The algorithm's approximate tithi
/// calculation may differ from panchanga-exact dates by zero, one, or two days; row tolerances therefore
/// admit a +/- 2 day window.
/// </para>
/// <para>
/// Known-date rows are restricted to years without an intercalary (Adhik) month immediately preceding the
/// target Hindu lunar month — in those years the algorithm locates the wrong lunation and returns a date
/// approximately one synodic month from the correct value. Those years are deliberately excluded from each
/// festival's known-answer table.
/// </para>
/// <para>
/// The shared boundary contract (year &lt; 1, year &gt; 9999, null/Gregorian/Julian calendar) is exercised
/// from <see cref="NotableDateAlgorithmContractTests" /> via the
/// <see cref="NotableDateAlgorithmKnownAnswers.AllAlgorithmFactories" /> enrollment, which enrolls a single
/// Kartik / Krishna / 15 instance because boundary behaviour is independent of the festival arguments.
/// </para>
/// </remarks>
[TestClass]
public sealed class HinduLunarNotableDateAlgorithmTests
{
    /// <summary>
    /// Verifies that an undefined <see cref="HinduLunarMonth" /> value throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMonthIsUndefined_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new HinduLunarNotableDateAlgorithm((HinduLunarMonth)99, HinduPaksha.Shukla, 1);
        });

        Assert.AreEqual("month", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="HinduPaksha" /> value throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPakshaIsUndefined_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, (HinduPaksha)99, 1);
        });

        Assert.AreEqual("paksha", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a tithi outside the range 1-15 throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="tithi">The out-of-range tithi value.</param>
    [DataRow(0)]
    [DataRow(16)]
    [TestMethod]
    public void Ctor_WhenTithiOutOfRange_ShouldThrowExactly(int tithi)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, HinduPaksha.Krishna, tithi);
        });

        Assert.AreEqual("tithi", ex.ParamName);
    }

    /// <summary>
    /// Verifies that Diwali (Amavasya of Kartik) falls within +/- 2 days of the published panchanga date for
    /// the smoke-tier representative year.
    /// </summary>
    /// <param name="knownAnswer">The Diwali known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.HinduLunarDiwaliSmoke" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.HinduLunarDiwaliSmoke),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenDiwali_ShouldFallWithinTwoDaysOfKnownPanchangaDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that Diwali falls within +/- 2 days of the published panchanga date for every row in the
    /// full known-answer table. Runs only under the Regression tier.
    /// </summary>
    /// <param name="knownAnswer">The Diwali known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.HinduLunarDiwali" />.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.HinduLunarDiwali),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenDiwaliAllRows_ShouldFallWithinTwoDaysOfKnownPanchangaDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that Holi (Purnima of Phalguna) falls within +/- 2 days of the published panchanga date for
    /// the smoke-tier representative year.
    /// </summary>
    /// <param name="knownAnswer">The Holi known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.HinduLunarHoliSmoke" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.HinduLunarHoliSmoke),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenHoli_ShouldFallWithinTwoDaysOfKnownPanchangaDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that Holi falls within +/- 2 days of the published panchanga date for every row in the full
    /// known-answer table. Runs only under the Regression tier.
    /// </summary>
    /// <param name="knownAnswer">The Holi known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.HinduLunarHoli" />.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.HinduLunarHoli),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenHoliAllRows_ShouldFallWithinTwoDaysOfKnownPanchangaDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that Navaratri start (Pratipada of Ashvin) falls within +/- 2 days of the published
    /// panchanga date for the smoke-tier representative year.
    /// </summary>
    /// <param name="knownAnswer">The Navaratri known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.HinduLunarNavaratriSmoke" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.HinduLunarNavaratriSmoke),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenNavaratri_ShouldFallWithinTwoDaysOfKnownPanchangaDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that Navaratri start falls within +/- 2 days of the published panchanga date for every row
    /// in the full known-answer table. Runs only under the Regression tier.
    /// </summary>
    /// <param name="knownAnswer">The Navaratri known-answer row supplied by
    /// <see cref="NotableDateAlgorithmKnownAnswers.HinduLunarNavaratri" />.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.HinduLunarNavaratri),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenNavaratriAllRows_ShouldFallWithinTwoDaysOfKnownPanchangaDate(NotableDateAlgorithmKnownAnswer knownAnswer) =>
        NotableDateAlgorithmAssertions.AssertResultWithinTolerance(knownAnswer);

    /// <summary>
    /// Verifies that every defined <see cref="HinduLunarMonth" /> resolves a non-null Gregorian date for a
    /// representative year. Exercises every branch of the private <c>GetSearchMonth</c> switch and ensures
    /// the algorithm produces a valid result regardless of which lunar month is requested.
    /// </summary>
    /// <param name="month">The Hindu lunar month under test.</param>
    [DataRow(HinduLunarMonth.Chaitra)]
    [DataRow(HinduLunarMonth.Vaisakha)]
    [DataRow(HinduLunarMonth.Jyeshtha)]
    [DataRow(HinduLunarMonth.Ashadha)]
    [DataRow(HinduLunarMonth.Shravana)]
    [DataRow(HinduLunarMonth.Bhadrapada)]
    [DataRow(HinduLunarMonth.Ashvin)]
    [DataRow(HinduLunarMonth.Kartik)]
    [DataRow(HinduLunarMonth.Margashirsha)]
    [DataRow(HinduLunarMonth.Pausha)]
    [DataRow(HinduLunarMonth.Magha)]
    [DataRow(HinduLunarMonth.Phalguna)]
    [TestMethod]
    public void GetDate_WhenEveryDefinedMonthAtTithiOne_ShouldReturnNonNull(HinduLunarMonth month)
    {
        var sut = new HinduLunarNotableDateAlgorithm(month, HinduPaksha.Shukla, 1);

        DateTime? result = sut.GetDate(2024);

        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Verifies that requesting the <see cref="HinduLunarMonth.Pausha" /> month for year 1 clamps the search
    /// year so the search is not attempted in year 0, returning a valid date.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenPaushaRequestedForYearOne_ShouldClampSearchYearAndReturnDate()
    {
        var sut = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Pausha, HinduPaksha.Shukla, 1);

        DateTime? result = sut.GetDate(1);

        Assert.IsNotNull(result);
    }
}
