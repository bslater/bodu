// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateQueryExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the by-year service overload and the month/year query conveniences over <see cref="DateOnly" />.
/// </summary>
[TestClass]
public sealed class NotableDateQueryExtensionsTests
{
    /// <summary>
    /// A resource with one holiday in January and one observance in May.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.query">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="labour-day" displayName="Labour Day" category="Observance" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Fixed month="May" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Gets a service over the fixture.
    /// </summary>
    private static INotableDateService Service =>
        new NotableDateService(NotableDateResourceLoader.Load(Xml));

    /// <summary>
    /// Verifies that the by-year overload resolves every occurrence emitted in the civil year.
    /// </summary>
    [TestMethod]
    public void Resolve_ByYear_ShouldReturnAllOccurrencesInYear()
    {
        Assert.HasCount(2, Service.Resolve(2026, "XX"));
    }

    /// <summary>
    /// Verifies that the filtered by-year overload restricts the result to matching occurrences.
    /// </summary>
    [TestMethod]
    public void Resolve_ByYearWithFilter_ShouldRestrictToMatches()
    {
        NotableDate match = Service.Resolve(2026, "XX", NotableDateFilter.ForCategory(NotableDateCategory.Observance)).Single();

        Assert.AreEqual("labour-day", match.NotableDateId);
    }

    /// <summary>
    /// Verifies that the month query returns only occurrences emitted within the date's calendar month.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInMonth_ShouldReturnOnlyThatMonth()
    {
        NotableDate match = new DateOnly(2026, 1, 15).GetNotableDatesInMonth(Service, "XX").Single();

        Assert.AreEqual("new-years-day", match.NotableDateId);
    }

    /// <summary>
    /// Verifies that the year query returns every occurrence emitted within the date's calendar year.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInYear_ShouldReturnWholeYear()
    {
        Assert.HasCount(2, new DateOnly(2026, 6, 1).GetNotableDatesInYear(Service, "XX"));
    }
}
