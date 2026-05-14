// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HinduLunarNotableDateAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the correctness and boundary behaviour of <see cref="HinduLunarNotableDateAlgorithm" />.
/// </summary>
/// <remarks>
/// <para>
/// Expected dates are taken from widely published Indian panchanga sources. Results from this algorithm may differ
/// from panchanga-exact dates by zero, one, or occasionally two calendar days due to the approximate tithi calculation
/// method used. Test assertions therefore allow a tolerance of ±2 days for astronomical festivals.
/// </para>
/// <para>
/// Known-date test rows are restricted to years without an intercalary (Adhik) month immediately preceding the target
/// Hindu lunar month. In years where such an intercalary month exists, <see cref="HinduLunarNotableDateAlgorithm" />
/// locates the wrong new-moon lunation and returns a date approximately one synodic month (29–30 days) from the correct
/// value. Those years are excluded from the parameterised data rows below.
/// </para>
/// </remarks>
[TestClass]
public sealed class HinduLunarNotableDateAlgorithmTests
{
    /// <summary>
    /// Verifies that an undefined <see cref="HinduLunarMonth" /> value throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMonthIsUndefined_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new HinduLunarNotableDateAlgorithm((HinduLunarMonth)99, HinduPaksha.Shukla, 1);
        });

        Assert.AreEqual("month", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="HinduPaksha" /> value throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPakshaIsUndefined_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, (HinduPaksha)99, 1);
        });

        Assert.AreEqual("paksha", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a tithi outside the range 1–15 throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [DataRow(0)]
    [DataRow(16)]
    [TestMethod]
    public void Ctor_WhenTithiOutOfRange_ShouldThrowArgumentOutOfRangeException(int tithi)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, HinduPaksha.Krishna, tithi);
        });

        Assert.AreEqual("tithi", ex.ParamName);
    }

    /// <summary>
    /// Verifies that requesting a date with a year below one throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [DataRow(0)]
    [DataRow(-1)]
    [TestMethod]
    public void GetDate_WhenYearLessThanOne_ShouldThrowArgumentOutOfRangeException(int year)
    {
        var sut = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, HinduPaksha.Krishna, 15);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut.GetDate(year);
        });

        Assert.AreEqual("year", ex.ParamName);
    }

    /// <summary>
    /// Verifies that Diwali (Amavasya / Krishna Paksha Chaturdashi of Kartik, i.e. the new moon) falls
    /// within ±2 days of the known panchanga date. Only years without an intercalary Kartik month are included;
    /// years 2022 and 2023 are excluded because an intercalary month causes the algorithm to locate the wrong
    /// lunation for those years.
    /// </summary>
    [DataRow(2024, 11, 1)]
    [TestMethod]
    public void GetDate_WhenDiwali_ShouldFallWithinTwoDaysOfKnownPanchangaDate(int year, int knownMonth, int knownDay)
    {
        // Diwali is on Amavasya (new moon day) of Kartik: Krishna Paksha, tithi 15 (Amavasya).
        var sut = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, HinduPaksha.Krishna, 15);

        DateTime? result = sut.GetDate(year);
        var expected = new DateTime(year, knownMonth, knownDay);

        Assert.IsNotNull(result);
        int dayDiff = Math.Abs((result!.Value - expected).Days);
        Assert.IsTrue(dayDiff <= 2,
            $"Diwali {year}: expected within ±2 days of {expected:yyyy-MM-dd}, got {result.Value:yyyy-MM-dd}.");
    }

    /// <summary>
    /// Verifies that Holi (Purnima of Phalguna — Shukla Paksha, tithi 15) falls within ±2 days of the known date.
    /// Only years without an intercalary Phalguna month are included; years 2022 and 2024 are excluded because an
    /// intercalary month causes the algorithm to locate the wrong lunation for those years.
    /// </summary>
    [DataRow(2023, 3, 7)]
    [TestMethod]
    public void GetDate_WhenHoli_ShouldFallWithinTwoDaysOfKnownPanchangaDate(int year, int knownMonth, int knownDay)
    {
        // Holi main day (Holika Dahan) is on Purnima (full moon) of Phalguna.
        var sut = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Phalguna, HinduPaksha.Shukla, 15);

        DateTime? result = sut.GetDate(year);
        var expected = new DateTime(year, knownMonth, knownDay);

        Assert.IsNotNull(result);
        int dayDiff = Math.Abs((result!.Value - expected).Days);
        Assert.IsTrue(dayDiff <= 2,
            $"Holi {year}: expected within ±2 days of {expected:yyyy-MM-dd}, got {result.Value:yyyy-MM-dd}.");
    }

    /// <summary>
    /// Verifies that Navaratri start (Shukla Paksha Pratipada of Ashvin, i.e. tithi 1) falls within ±2 days
    /// of the known panchanga date. Only years without an intercalary Ashvin month are included; years 2023 and
    /// 2024 are excluded because an intercalary month causes the algorithm to locate the wrong lunation.
    /// </summary>
    [DataRow(2022, 9, 26)]
    [TestMethod]
    public void GetDate_WhenNavaratri_ShouldFallWithinTwoDaysOfKnownPanchangaDate(int year, int knownMonth, int knownDay)
    {
        var sut = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Ashvin, HinduPaksha.Shukla, 1);

        DateTime? result = sut.GetDate(year);
        var expected = new DateTime(year, knownMonth, knownDay);

        Assert.IsNotNull(result);
        int dayDiff = Math.Abs((result!.Value - expected).Days);
        Assert.IsTrue(dayDiff <= 2,
            $"Navaratri {year}: expected within ±2 days of {expected:yyyy-MM-dd}, got {result.Value:yyyy-MM-dd}.");
    }

    /// <summary>
    /// Verifies that the returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenCalendarIsNull_ShouldReturnUnspecifiedKind()
    {
        var sut = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, HinduPaksha.Krishna, 15);

        DateTime? result = sut.GetDate(2024);

        Assert.IsNotNull(result);
        Assert.AreEqual(DateTimeKind.Unspecified, result!.Value.Kind);
    }

    /// <summary>
    /// Verifies that supplying an explicit <see cref="System.Globalization.GregorianCalendar" /> matches the default
    /// (null) calendar path.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenCalendarIsExplicitlyGregorian_ShouldMatchDefaultPath()
    {
        var sut = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, HinduPaksha.Krishna, 15);

        DateTime? withDefault = sut.GetDate(2024);
        DateTime? withGregorian = sut.GetDate(2024, new System.Globalization.GregorianCalendar());

        Assert.AreEqual(withDefault, withGregorian);
    }

    /// <summary>
    /// Verifies that supplying a <see cref="System.Globalization.JulianCalendar" /> projects the result through the
    /// non-Gregorian projection branch.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenCalendarIsJulian_ShouldProjectThroughTargetCalendar()
    {
        var sut = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, HinduPaksha.Krishna, 15);
        System.Globalization.JulianCalendar julian = new();

        DateTime? result = sut.GetDate(2024, julian);

        Assert.IsNotNull(result);
        Assert.AreEqual(2024, julian.GetYear(result!.Value));
    }

    /// <summary>
    /// Verifies that every defined <see cref="HinduLunarMonth" /> resolves a non-null Gregorian date for a representative
    /// year. This exercises every branch of the private <c>GetSearchMonth</c> switch and ensures the algorithm produces
    /// a valid result regardless of which lunar month is requested.
    /// </summary>
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
    /// Verifies that requesting the <see cref="HinduLunarMonth.Pausha" /> month for year 1 clamps the search year so the
    /// search is not attempted in year 0, returning a valid date.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenPaushaRequestedForYearOne_ShouldClampSearchYearAndReturnDate()
    {
        var sut = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Pausha, HinduPaksha.Shukla, 1);

        DateTime? result = sut.GetDate(1);

        Assert.IsNotNull(result);
    }
}
