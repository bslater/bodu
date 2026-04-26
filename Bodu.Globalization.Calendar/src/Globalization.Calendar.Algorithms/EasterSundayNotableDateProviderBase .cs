// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EasterSundayNotableDateProviderBase .cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides a base implementation for Easter Sunday notable date providers.
/// </summary>
public abstract class EasterSundayNotableDateProviderBase : INotableDateProvider
{
    private readonly ConcurrentDictionary<int, DateTime> _dateCache = new();

    /// <summary>
    /// Gets the canonical notable date name produced by the provider.
    /// </summary>
    protected abstract string Name { get; }

    /// <summary>
    /// Gets the category assigned to the produced notable date.
    /// </summary>
    protected abstract NotableDateCategory Category { get; }

    /// <inheritdoc />
    public abstract int MinSupportedYear { get; }

    /// <inheritdoc />
    public virtual int MaxSupportedYear => 9999;

    /// <summary>
    /// Gets the comment to attach to the produced notable date, if any.
    /// </summary>
    protected virtual string? Comment => null;

    /// <inheritdoc />
    public bool SupportsYear(int year) => year >= MinSupportedYear && year <= MaxSupportedYear;

    /// <summary>
    /// Gets the tags to attach to the produced notable date.
    /// </summary>
    protected virtual ImmutableHashSet<string> Tags =>
        ImmutableHashSet.Create<string>(
            StringComparer.OrdinalIgnoreCase,
            "Christianity",
            "Easter");

    /// <summary>
    /// Gets the default calendar type associated with the provider, or <see langword="null" /> when no explicit calendar should be
    /// attached unless supplied by the caller.
    /// </summary>
    protected virtual Type? DefaultCalendarType => null;

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> GetDates(int year, SysGlobal.Calendar? calendar = null)
    {
        ThrowHelper.ThrowIfLessThan(year, 1);

        ValidateCalendar(calendar);

        DateTime date = _dateCache.GetOrAdd(year, CalculateDate);

        return
        [
            new NotableDate
                {
                    Date = date,
                    Name = Name,
                    Category = Category,
                    CalendarType = calendar?.GetType() ?? DefaultCalendarType,
                    Tags = Tags,
                    Comment = Comment,
                    DurationDays = 1,
                    IsNonWorkingDay = false,
                },
            ];
    }

    /// <summary>
    /// Validates the optional calendar supplied to the provider.
    /// </summary>
    /// <param name="calendar">The calendar to validate.</param>
    /// <exception cref="NotSupportedException">Thrown when the calendar is not supported by the provider.</exception>
    protected abstract void ValidateCalendar(SysGlobal.Calendar? calendar);

    /// <summary>
    /// Calculates the Easter Sunday date for the specified year.
    /// </summary>
    /// <param name="year">The target year.</param>
    /// <returns>The resolved Easter Sunday date.</returns>
    protected abstract DateTime CalculateDate(int year);
}