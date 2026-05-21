// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsalhaPujaNotableDateAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the correctness and boundary behaviour of <see cref="AsalhaPujaNotableDateAlgorithm" />.
/// </summary>
[TestClass]
public sealed class AsalhaPujaNotableDateAlgorithmTests
{
    private readonly AsalhaPujaNotableDateAlgorithm _algorithm = new();

    /// <summary>
    /// Verifies that requesting Asalha Puja with a year below one throws <see cref="ArgumentOutOfRangeException" />.
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
    /// Verifies that requesting Asalha Puja with a year above 9999 throws <see cref="ArgumentOutOfRangeException" />
    /// rather than a raw exception from the <see cref="DateTime" /> constructor.
    /// </summary>
    [DataRow(10000)]
    [DataRow(int.MaxValue)]
    [TestMethod]
    public void GetDate_WhenYearGreaterThan9999_ShouldThrowArgumentOutOfRangeException(int year)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = _algorithm.GetDate(year);
        });

        Assert.AreEqual("year", ex.ParamName);
    }

    /// <summary>
    /// Verifies that Asalha Puja returns known correct dates for years where the first full moon on or after
    /// 15 June matches the Thai Asanha Bucha public holiday. Years with Thai intercalary months (such as 2023 and
    /// 2024) are excluded because the official observance is moved to the following lunation.
    /// </summary>
    [DataRow(2022, 7, 13)]
    [DataRow(2025, 7, 10)]
    [TestMethod]
    public void GetDate_WhenGivenKnownYears_ShouldReturnExpectedDate(int year, int expectedMonth, int expectedDay)
    {
        DateTime? result = _algorithm.GetDate(year);

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value,
            $"Asalha Puja {year}: expected {expectedMonth:D2}/{expectedDay:D2}, got {result.Value:yyyy-MM-dd}");
    }

    /// <summary>
    /// Verifies that for every year in the range 1901–2100 the result falls in June or July, consistent with
    /// the definition of the first full moon on or after 15 June.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenIteratingSupportedRange_ShouldAlwaysFallInJuneOrJuly()
    {
        for (var year = 1901; year <= 2100; year++)
        {
            DateTime? result = _algorithm.GetDate(year);

            Assert.IsNotNull(result, $"GetDate returned null for year {year}.");
            Assert.IsTrue(result!.Value.Month is 6 or 7,
                $"Expected June or July for year {year}, got month {result.Value.Month}.");
            Assert.IsTrue(result.Value >= new DateTime(year, 6, 15),
                $"Expected date on or after 15 June for year {year}, got {result.Value:yyyy-MM-dd}.");
        }
    }

    /// <summary>
    /// Verifies that the returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenCalendarIsNull_ShouldReturnUnspecifiedKind()
    {
        DateTime? result = _algorithm.GetDate(2022);

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
