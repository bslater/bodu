// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.AmbientFilterOverloads.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

/// <summary>
/// Verifies that the ambient-service overloads of <see cref="NotableDateTimeExtensions" /> that accept an
/// explicit <see cref="NotableDateFilter" /> delegate correctly to the fuller signature using
/// <see cref="NotableDateContext.Default" />.
/// </summary>
public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that the ambient-service overload of
    /// <see cref="NotableDateTimeExtensions.GetNotableDates(DateTime, NotableDateFilter, string?, Type?)" />
    /// routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_AmbientFilterOverload_ShouldUseDefaultContext()
    {
        NotableDateService service = BuildService(
            Fixed("Holiday", 5, 14, NotableDateCategory.Holiday),
            Fixed("Cultural", 5, 14, NotableDateCategory.Cultural));
        try
        {
            NotableDateContext.Default = service;
            var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);

            IReadOnlyList<NotableDate> resolved = new DateTime(2026, 5, 14).GetNotableDates(filter);

            Assert.HasCount(1, resolved);
            Assert.AreEqual("Holiday", resolved[0].Name);
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateTimeExtensions.GetNotableDatesInMonth(DateTime, NotableDateFilter, string?, Type?)" />
    /// returns only filter-matching dates in the same calendar month as the supplied date when routed through the
    /// ambient context.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInMonth_AmbientFilterOverload_ShouldUseDefaultContext()
    {
        NotableDateService service = BuildService(
            Fixed("MayHoliday", 5, 14, NotableDateCategory.Holiday),
            Fixed("MayCultural", 5, 28, NotableDateCategory.Cultural));
        try
        {
            NotableDateContext.Default = service;
            var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);

            IReadOnlyList<NotableDate> resolved = new DateTime(2026, 5, 1).GetNotableDatesInMonth(filter);

            Assert.HasCount(1, resolved);
            Assert.AreEqual("MayHoliday", resolved[0].Name);
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateTimeExtensions.GetNotableDatesInYear(DateTime, NotableDateFilter, string?, Type?)" />
    /// returns only filter-matching dates in the same calendar year as the supplied date when routed through the
    /// ambient context.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInYear_AmbientFilterOverload_ShouldUseDefaultContext()
    {
        NotableDateService service = BuildService(
            Fixed("Spring", 3, 1, NotableDateCategory.Holiday),
            Fixed("Autumn", 9, 1, NotableDateCategory.Cultural));
        try
        {
            NotableDateContext.Default = service;
            var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);

            IReadOnlyList<NotableDate> resolved = new DateTime(2026, 6, 1).GetNotableDatesInYear(filter);

            Assert.HasCount(1, resolved);
            Assert.AreEqual("Spring", resolved[0].Name);
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateTimeExtensions.NextNotableDate(DateTime, NotableDateFilter, string?, Type?)" />
    /// returns the next filter-matching date strictly after the supplied date, routed through the ambient context.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_AmbientFilterOverload_ShouldUseDefaultContext()
    {
        NotableDateService service = BuildService(
            Fixed("Cultural", 5, 1, NotableDateCategory.Cultural),
            Fixed("Holiday", 6, 1, NotableDateCategory.Holiday));
        try
        {
            NotableDateContext.Default = service;
            var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);

            NotableDate? next = new DateTime(2026, 4, 1).NextNotableDate(filter);

            Assert.IsNotNull(next);
            Assert.AreEqual("Holiday", next!.Name);
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateTimeExtensions.PreviousNotableDate(DateTime, NotableDateFilter, string?, Type?)" />
    /// returns the previous filter-matching date strictly before the supplied date, routed through the ambient
    /// context.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_AmbientFilterOverload_ShouldUseDefaultContext()
    {
        NotableDateService service = BuildService(
            Fixed("Cultural", 5, 1, NotableDateCategory.Cultural),
            Fixed("Holiday", 4, 1, NotableDateCategory.Holiday));
        try
        {
            NotableDateContext.Default = service;
            var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);

            NotableDate? previous = new DateTime(2026, 6, 1).PreviousNotableDate(filter);

            Assert.IsNotNull(previous);
            Assert.AreEqual("Holiday", previous!.Name);
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateTimeExtensions.IsNotableDate(DateTime, NotableDateFilter, string?, Type?)" />
    /// returns <see langword="true" /> when at least one filter-matching notable date covers the supplied date.
    /// </summary>
    [TestMethod]
    public void IsNotableDate_AmbientFilterOverload_ShouldUseDefaultContext()
    {
        NotableDateService service = BuildService(
            Fixed("Holiday", 5, 14, NotableDateCategory.Holiday));
        try
        {
            NotableDateContext.Default = service;
            var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);

            Assert.IsTrue(new DateTime(2026, 5, 14).IsNotableDate(filter));
            Assert.IsFalse(new DateTime(2026, 5, 15).IsNotableDate(filter));
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }
}
