// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.ConvenienceOverloads.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

/// <summary>
/// Verifies that the convenience overloads of <see cref="NotableDateTimeExtensions" /> — overloads that do not
/// accept an <see cref="INotableDateService" /> argument — route through <see cref="NotableDateContext.Default" />.
/// Mirrors the DateOnly-side convenience tests so both public extension surfaces have parity.
/// </summary>
public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that the ambient-service overload of <c>EnumerateWorkingDays</c> uses
    /// <see cref="NotableDateContext.Default" /> to classify the supplied range.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_AmbientOverload_ShouldUseDefaultContext()
    {
        NotableDateService service = BuildService();
        try
        {
            NotableDateContext.Default = service;

            IEnumerable<DateTime> working = new DateTime(2026, 1, 5)
                .EnumerateWorkingDays(new DateTime(2026, 1, 9));

            Assert.AreEqual(5, System.Linq.Enumerable.Count(working));
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that the ambient-service overload of <c>EnumerateNonWorkingDays</c> uses
    /// <see cref="NotableDateContext.Default" /> to classify the supplied range.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_AmbientOverload_ShouldUseDefaultContext()
    {
        NotableDateService service = BuildService();
        try
        {
            NotableDateContext.Default = service;

            IEnumerable<DateTime> nonWorking = new DateTime(2026, 1, 5)
                .EnumerateNonWorkingDays(new DateTime(2026, 1, 11));

            Assert.AreEqual(2, System.Linq.Enumerable.Count(nonWorking));
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that the ambient-service overload of <c>EnumerateNotableDates</c> uses
    /// <see cref="NotableDateContext.Default" /> for resolution.
    /// </summary>
    [TestMethod]
    public void EnumerateNotableDates_AmbientOverload_ShouldUseDefaultContext()
    {
        NotableDateService service = BuildService(Fixed("Anchor", 4, 1));
        try
        {
            NotableDateContext.Default = service;

            IEnumerable<NotableDate> resolved = new DateTime(2026, 1, 1)
                .EnumerateNotableDates(new DateTime(2026, 12, 31));

            Assert.IsTrue(System.Linq.Enumerable.Any(resolved, d => d.Name == "Anchor"));
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that the ambient-service overload of <c>EnumerateNotableDates</c> applies the supplied filter.
    /// </summary>
    [TestMethod]
    public void EnumerateNotableDates_AmbientFilterOverload_ShouldApplyFilter()
    {
        NotableDateService service = BuildService(
            Fixed("Holiday", 4, 1, NotableDateCategory.Holiday),
            Fixed("Cultural Day", 5, 1, NotableDateCategory.Cultural));
        try
        {
            NotableDateContext.Default = service;

            IEnumerable<NotableDate> filtered = new DateTime(2026, 1, 1)
                .EnumerateNotableDates(
                    new DateTime(2026, 12, 31),
                    NotableDateFilter.ForCategory(NotableDateCategory.Cultural));

            var list = System.Linq.Enumerable.ToList(filtered);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("Cultural Day", list[0].Name);
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that the ambient-service overload of <c>GetNotableDatesInMonth</c> returns the dates for the calendar
    /// month of the supplied reference value, using month-boundary derivation.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInMonth_AmbientOverload_ShouldUseMonthBoundaries()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 5, 15));
        try
        {
            NotableDateContext.Default = service;

            IReadOnlyList<NotableDate> may = new DateTime(2026, 5, 1).GetNotableDatesInMonth();

            Assert.AreEqual(1, may.Count);
            Assert.AreEqual("Holiday", may[0].Name);
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that the ambient-service overload of <c>GetNotableDatesInYear</c> returns the dates for the calendar
    /// year of the supplied reference value, using year-boundary derivation.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInYear_AmbientOverload_ShouldUseYearBoundaries()
    {
        NotableDateService service = BuildService(Fixed("New Year's Day", 1, 1));
        try
        {
            NotableDateContext.Default = service;

            IReadOnlyList<NotableDate> resolved = new DateTime(2026, 7, 1).GetNotableDatesInYear();

            Assert.IsTrue(System.Linq.Enumerable.Any(resolved, d => d.Name == "New Year's Day"));
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that <c>IsNotableDate</c> with a filter returns <see langword="true" /> when at least one matching
    /// notable date covers the day.
    /// </summary>
    [TestMethod]
    public void IsNotableDate_WithFilter_WhenFilterMatches_ShouldReturnTrue()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 4, 1, NotableDateCategory.Holiday));
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);

        var result = new DateTime(2026, 4, 1).IsNotableDate(service, filter);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that <c>IsNotableDate</c> with a filter returns <see langword="false" /> when no matching notable date
    /// covers the day.
    /// </summary>
    [TestMethod]
    public void IsNotableDate_WithFilter_WhenFilterDoesNotMatch_ShouldReturnFalse()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 4, 1, NotableDateCategory.Holiday));
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Cultural);

        var result = new DateTime(2026, 4, 1).IsNotableDate(service, filter);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that <c>NextNotableDate</c> with a filter returns <see langword="null" /> when no rule matches the
    /// supplied filter within the supported year range.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WithFilter_WhenNoMatchExists_ShouldReturnNull()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 4, 1, NotableDateCategory.Holiday));
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Cultural);

        NotableDate? result = new DateTime(2026, 1, 1).NextNotableDate(service, filter);

        Assert.IsNull(result);
    }
}
