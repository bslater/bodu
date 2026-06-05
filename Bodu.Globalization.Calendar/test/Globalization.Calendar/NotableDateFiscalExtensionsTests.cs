// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFiscalExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateFiscalExtensions" /> resolves the boundary working days of the fiscal year and
/// quarter that contain a date.
/// </summary>
[TestClass]
public sealed class NotableDateFiscalExtensionsTests
{
    /// <summary>
    /// A resource declaring no notable dates, so working days are governed only by the working week.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.fiscal">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
    </NotableDateResource>
    """;

    /// <summary>
    /// Gets a service over the fixture.
    /// </summary>
    private static INotableDateService Service =>
        new NotableDateService(NotableDateResourceLoader.Load(Xml));

    /// <summary>
    /// Verifies that a July fiscal year resolves its first working day to 1 July when that is a weekday.
    /// </summary>
    [TestMethod]
    public void FirstWorkingDayOfFiscalYear_JulyStart_ShouldReturnFirstOfJuly()
    {
        // 1 July 2025 is a Tuesday.
        DateOnly result = new DateOnly(2025, 9, 15).FirstWorkingDayOfFiscalYear(7, Service, "XX");

        Assert.AreEqual(new DateOnly(2025, 7, 1), result);
    }

    /// <summary>
    /// Verifies that a fiscal year beginning on a weekend snaps its first working day forward.
    /// </summary>
    [TestMethod]
    public void FirstWorkingDayOfFiscalYear_WhenStartIsWeekend_ShouldSnapForward()
    {
        // A June fiscal year starts 1 June 2025, which is a Sunday, so the first working day is Monday 2 June.
        DateOnly result = new DateOnly(2025, 8, 15).FirstWorkingDayOfFiscalYear(6, Service, "XX");

        Assert.AreEqual(new DateOnly(2025, 6, 2), result);
    }

    /// <summary>
    /// Verifies that a July fiscal year resolves its last working day to 30 June of the following year.
    /// </summary>
    [TestMethod]
    public void LastWorkingDayOfFiscalYear_JulyStart_ShouldReturnEndOfJune()
    {
        // 30 June 2026 is a Tuesday.
        DateOnly result = new DateOnly(2025, 9, 15).LastWorkingDayOfFiscalYear(7, Service, "XX");

        Assert.AreEqual(new DateOnly(2026, 6, 30), result);
    }

    /// <summary>
    /// Verifies that the first quarter of a July fiscal year spans July to September.
    /// </summary>
    [TestMethod]
    public void FirstWorkingDayOfFiscalQuarter_JulyStart_ShouldReturnFirstOfJuly()
    {
        DateOnly result = new DateOnly(2025, 8, 15).FirstWorkingDayOfFiscalQuarter(7, Service, "XX");

        Assert.AreEqual(new DateOnly(2025, 7, 1), result);
    }

    /// <summary>
    /// Verifies that the last working day of the first fiscal quarter is the end of September.
    /// </summary>
    [TestMethod]
    public void LastWorkingDayOfFiscalQuarter_JulyStart_ShouldReturnEndOfSeptember()
    {
        // 30 September 2025 is a Tuesday.
        DateOnly result = new DateOnly(2025, 8, 15).LastWorkingDayOfFiscalQuarter(7, Service, "XX");

        Assert.AreEqual(new DateOnly(2025, 9, 30), result);
    }

    /// <summary>
    /// Verifies that a fiscal-year start month outside 1 to 12 is rejected.
    /// </summary>
    [TestMethod]
    public void FirstWorkingDayOfFiscalYear_WhenMonthOutOfRange_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DateOnly(2025, 9, 15).FirstWorkingDayOfFiscalYear(13, Service, "XX");
        });
    }
}
