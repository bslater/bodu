// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateServiceDefaultMembersTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the default-implemented members of <see cref="INotableDateService" /> against a minimal stub
/// implementation that does not override them. These tests exercise the default interface method bodies — which
/// would otherwise be shadowed by every override on the canonical <see cref="NotableDateService" /> implementation.
/// </summary>
[TestClass]
public sealed class INotableDateServiceDefaultMembersTests
{
    /// <summary>
    /// Verifies that the default implementation of <see cref="INotableDateService.WorkingWeek" /> reports
    /// <see cref="WeekPattern.Weekdays" />.
    /// </summary>
    [TestMethod]
    public void WorkingWeek_WhenImplementorDoesNotOverride_ShouldDefaultToWeekdays()
    {
        INotableDateService service = new MinimalNotableDateService();

        Assert.AreEqual(WeekPattern.Weekdays, service.WorkingWeek);
    }

    /// <summary>
    /// Verifies that the default implementation of <see cref="INotableDateService.GetSupportedTerritories" /> returns
    /// an empty collection.
    /// </summary>
    [TestMethod]
    public void GetSupportedTerritories_WhenImplementorDoesNotOverride_ShouldReturnEmpty()
    {
        INotableDateService service = new MinimalNotableDateService();

        Assert.AreEqual(0, service.GetSupportedTerritories().Count);
    }

    /// <summary>
    /// Verifies that the default implementation of <see cref="INotableDateService.GetSupportedCalendars" /> returns
    /// an empty collection.
    /// </summary>
    [TestMethod]
    public void GetSupportedCalendars_WhenImplementorDoesNotOverride_ShouldReturnEmpty()
    {
        INotableDateService service = new MinimalNotableDateService();

        Assert.AreEqual(0, service.GetSupportedCalendars().Count);
    }

    /// <summary>
    /// Verifies that the default implementation of <see cref="INotableDateService.Reload" /> delegates to
    /// <see cref="INotableDateService.Invalidate()" /> exactly once.
    /// </summary>
    [TestMethod]
    public void Reload_WhenImplementorDoesNotOverride_ShouldDelegateToInvalidate()
    {
        MinimalNotableDateService implementation = new();
        INotableDateService service = implementation;

        service.Reload();

        Assert.AreEqual(1, implementation.InvalidateCallCount);
    }

    /// <summary>
    /// Verifies that the default implementation of <see cref="INotableDateService.IsHolidayNonWorkingDay" /> walks
    /// <see cref="INotableDateService.GetNotableDates(DateTime, string?, Type?)" /> and returns <see langword="true" />
    /// when any covering date is flagged non-working.
    /// </summary>
    [TestMethod]
    public void IsHolidayNonWorkingDay_WhenImplementorDoesNotOverrideAndCoveringDateIsNonWorking_ShouldReturnTrue()
    {
        DateTime date = new(2026, 6, 1);
        INotableDateService service = new MinimalNotableDateService
        {
            DatesForSingleDay =
            [
                new NotableDate { Date = date, Name = "X", Category = NotableDateCategory.Holiday, IsNonWorkingDay = false },
                new NotableDate { Date = date, Name = "Y", Category = NotableDateCategory.Holiday, IsNonWorkingDay = true },
            ],
        };

        Assert.IsTrue(service.IsHolidayNonWorkingDay(date));
    }

    /// <summary>
    /// Verifies that the default implementation of <see cref="INotableDateService.IsHolidayNonWorkingDay" /> returns
    /// <see langword="false" /> when no covering date is non-working.
    /// </summary>
    [TestMethod]
    public void IsHolidayNonWorkingDay_WhenImplementorDoesNotOverrideAndNoCoveringDateIsNonWorking_ShouldReturnFalse()
    {
        DateTime date = new(2026, 6, 1);
        INotableDateService service = new MinimalNotableDateService
        {
            DatesForSingleDay =
            [
                new NotableDate { Date = date, Name = "X", Category = NotableDateCategory.Holiday, IsNonWorkingDay = false },
            ],
        };

        Assert.IsFalse(service.IsHolidayNonWorkingDay(date));
    }

    /// <summary>
    /// Verifies that the default implementation of <see cref="INotableDateService.IsHolidayNonWorkingDay" /> returns
    /// <see langword="false" /> when no notable date covers the supplied day.
    /// </summary>
    [TestMethod]
    public void IsHolidayNonWorkingDay_WhenImplementorDoesNotOverrideAndNoCoveringDate_ShouldReturnFalse()
    {
        INotableDateService service = new MinimalNotableDateService { DatesForSingleDay = [] };

        Assert.IsFalse(service.IsHolidayNonWorkingDay(new DateTime(2026, 6, 1)));
    }

    /// <summary>
    /// Stub implementation of <see cref="INotableDateService" /> that overrides only the abstract members and lets the
    /// default interface methods run unmodified. Used by the per-test cases above to exercise the fallback bodies.
    /// </summary>
    private sealed class MinimalNotableDateService : INotableDateService
    {
        /// <summary>
        /// Gets the number of times <see cref="Invalidate()" /> has been called on this stub.
        /// </summary>
        public int InvalidateCallCount { get; private set; }

        /// <summary>
        /// Gets or sets the result returned from the single-day <c>GetNotableDates</c> overload. Used by
        /// <see cref="INotableDateService.IsHolidayNonWorkingDay" /> tests to drive the default body.
        /// </summary>
        public IReadOnlyList<NotableDate> DatesForSingleDay { get; set; } = [];

        /// <inheritdoc />
        public bool IsWeekend(DateTime date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        /// <inheritdoc />
        public bool IsNonWorkingDay(DateTime date, string? territoryCode = null, Type? calendarType = null) => IsWeekend(date);

        /// <inheritdoc />
        public IReadOnlyList<NotableDate> GetNotableDates(int year, string? territoryCode = null, Type? calendarType = null) => [];

        /// <inheritdoc />
        public IReadOnlyList<NotableDate> GetNotableDates(int year, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null) => [];

        /// <inheritdoc />
        public IReadOnlyList<NotableDate> GetNotableDates(DateTime startDate, DateTime endDate, string? territoryCode = null, Type? calendarType = null) => [];

        /// <inheritdoc />
        public IReadOnlyList<NotableDate> GetNotableDates(DateTime startDate, DateTime endDate, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null) => [];

        /// <inheritdoc />
        public IReadOnlyList<NotableDate> GetNotableDates(DateTime date, string? territoryCode = null, Type? calendarType = null) => DatesForSingleDay;

        /// <inheritdoc />
        public IReadOnlyList<NotableDate> GetNotableDates(DateTime date, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null) => DatesForSingleDay;

        /// <inheritdoc />
        public void Invalidate() => InvalidateCallCount++;
    }
}
