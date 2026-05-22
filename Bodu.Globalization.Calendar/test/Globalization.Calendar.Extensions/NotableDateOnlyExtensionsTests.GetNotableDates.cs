// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.GetNotableDates.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that a single-day query returns notable dates whose anchor or span covers the input day.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenDayMatchesAnchor_ShouldReturnMatchingNotableDate()
    {
        NotableDateService service = BuildService(Fixed("Christmas Day", 12, 25));

        IReadOnlyList<NotableDate> result = new DateOnly(2026, 12, 25).GetNotableDates(service);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Christmas Day", result[0].Name);
    }

    /// <summary>
    /// Verifies that a multi-day span is returned when querying any day inside the span.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenDayLiesInsideMultiDaySpan_ShouldReturnSpan()
    {
        NotableDateRule rule = Fixed("Festival", 6, 1) with { DurationDays = 5 };
        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> result = new DateOnly(2026, 6, 3).GetNotableDates(service);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Festival", result[0].Name);
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenServiceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 1).GetNotableDates(service: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> filter to the explicit-service overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenExplicitFilterIsNull_ShouldThrowExactly()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 1).GetNotableDates(service, filter: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> filter to the ambient overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenAmbientFilterIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 1).GetNotableDates(filter: null!);
        });
    }

    /// <summary>
    /// Verifies that a filter restricts the returned set to matching notable dates only.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenFilterApplied_ShouldReturnOnlyMatching()
    {
        NotableDateService service = BuildService(Fixed("Christmas Day", 12, 25, NotableDateCategory.Holiday));
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Cultural);

        IReadOnlyList<NotableDate> result = new DateOnly(2026, 12, 25).GetNotableDates(service, filter);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 1));
        try
        {
            NotableDateContext.Default = service;

            IReadOnlyList<NotableDate> result = new DateOnly(2026, 1, 1).GetNotableDates();

            Assert.AreEqual(1, result.Count);
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }
}
