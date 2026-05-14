// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LosarNotableDateAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the correctness and boundary behaviour of <see cref="LosarNotableDateAlgorithm" />.
/// </summary>
/// <remarks>
/// <para>
/// The Losar algorithm returns the first new moon on or after 20 January as an approximation of the Tibetan New Year.
/// In years where the Tibetan lunisolar calendar inserts an intercalary month before the first month, the algorithm
/// may return a date approximately one month earlier than the actual Losar. Known-date tests therefore select years
/// where the algorithm is known to agree with the official Tibetan calendar.
/// </para>
/// </remarks>
[TestClass]
public sealed class LosarNotableDateAlgorithmTests
{
    private readonly LosarNotableDateAlgorithm _algorithm = new();

    /// <summary>
    /// Verifies that requesting Losar with a year below one throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    [TestMethod]
    public void GetDate_WhenYearLessThanOne_ShouldThrowArgumentOutOfRangeException(int year)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = _algorithm.GetDate(year);
        });

        Assert.AreEqual("year", ex.ParamName);
    }

    /// <summary>
    /// Verifies that Losar returns a date within ±1 day of the known official Tibetan New Year for years where the
    /// algorithm agrees with the Tibetan lunisolar calendar. Years where the Tibetan calendar diverges by a full
    /// intercalary month are excluded.
    /// </summary>
    [DataRow(2021, 2, 12)]
    [DataRow(2024, 2, 10)]
    [TestMethod]
    public void GetDate_WhenGivenKnownYears_ShouldFallWithinOneDayOfOfficialDate(int year, int knownMonth, int knownDay)
    {
        DateTime? result = _algorithm.GetDate(year);
        var expected = new DateTime(year, knownMonth, knownDay);

        Assert.IsNotNull(result);
        int dayDiff = Math.Abs((result!.Value - expected).Days);
        Assert.IsTrue(dayDiff <= 1,
            $"Losar {year}: expected within ±1 day of {expected:yyyy-MM-dd}, got {result.Value:yyyy-MM-dd}.");
    }

    /// <summary>
    /// Verifies that for every year in the range 1901–2100 the result falls in January or February, consistent
    /// with the algorithm's definition of the first new moon on or after 20 January.
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

    /// <summary>
    /// Verifies that the returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenCalendarIsNull_ShouldReturnUnspecifiedKind()
    {
        DateTime? result = _algorithm.GetDate(2024);

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
        DateTime? withDefault = _algorithm.GetDate(2024);
        DateTime? withGregorian = _algorithm.GetDate(2024, new System.Globalization.GregorianCalendar());

        Assert.AreEqual(withDefault, withGregorian);
    }

    /// <summary>
    /// Verifies that supplying a <see cref="System.Globalization.JulianCalendar" /> projects the result through the
    /// non-Gregorian projection branch.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenCalendarIsJulian_ShouldProjectThroughTargetCalendar()
    {
        System.Globalization.JulianCalendar julian = new();

        DateTime? result = _algorithm.GetDate(2024, julian);

        Assert.IsNotNull(result);
        Assert.AreEqual(2024, julian.GetYear(result!.Value));
    }
}
