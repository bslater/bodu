// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EasterSundayNotableDateProviderBase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Collections.Immutable;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides a base implementation for Easter Sunday notable date providers.
/// </summary>
/// <remarks>
/// <para>
/// Derived classes supply the Easter Sunday calculation algorithm via <see cref="CalculateDate" /> and declare the
/// <see cref="Name" />, <see cref="Category" />, and optional <see cref="Comment" /> and
/// <see cref="DefaultCalendarType" /> properties. Results are cached per year in a thread-safe
/// <see cref="ConcurrentDictionary{TKey,TValue}" />.
/// </para>
/// </remarks>
public abstract class EasterSundayNotableDateProviderBase
    : INotableDateProvider
{
    /// <summary>
    /// Thread-safe per-year cache of computed Easter Sunday dates for this provider.
    /// </summary>
    private readonly ConcurrentDictionary<int, DateTime> _dateCache = new();

    /// <summary>
    /// Gets the canonical notable date name produced by this provider (e.g. <c>"Easter Sunday"</c>).
    /// </summary>
    /// <returns>The name assigned to the produced <see cref="NotableDate" />.</returns>
    protected abstract string Name { get; }

    /// <summary>
    /// Gets the primary category assigned to the produced <see cref="NotableDate" />.
    /// </summary>
    /// <returns>The <see cref="NotableDateCategory" /> for the produced date.</returns>
    protected abstract NotableDateCategory Category { get; }

    /// <inheritdoc />
    public abstract int MinSupportedYear { get; }

    /// <inheritdoc />
    public virtual int MaxSupportedYear => 9999;

    /// <summary>
    /// Gets the optional comment attached to the produced <see cref="NotableDate" />.
    /// </summary>
    /// <returns>A human-readable comment, or <see langword="null" /> when none is applicable.</returns>
    protected virtual string? Comment => null;

    /// <inheritdoc />
    public bool SupportsYear(int year) => year >= MinSupportedYear && year <= MaxSupportedYear;

    /// <summary>
    /// Gets the non-exclusive tags attached to the produced <see cref="NotableDate" />.
    /// </summary>
    /// <returns>
    /// An <see cref="ImmutableHashSet{T}" /> containing at least <c>"Christianity"</c> and <c>"Easter"</c>. Derived
    /// classes may override to add or replace tags.
    /// </returns>
    protected virtual ImmutableHashSet<string> Tags =>
        ImmutableHashSet.Create<string>(
            StringComparer.OrdinalIgnoreCase,
            "Christianity",
            "Easter");

    /// <summary>
    /// Gets the default calendar type associated with this provider, attached to each produced
    /// <see cref="NotableDate" /> when the caller does not supply an explicit calendar.
    /// </summary>
    /// <returns>
    /// The <see cref="Type" /> of the default calendar, or <see langword="null" /> when no explicit calendar should be
    /// attached.
    /// </returns>
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
    /// Validates the optional calendar supplied to <see cref="GetDates" />.
    /// </summary>
    /// <param name="calendar">The calendar to validate, or <see langword="null" /> for the default.</param>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="calendar" /> is not supported by this provider.
    /// </exception>
    protected abstract void ValidateCalendar(SysGlobal.Calendar? calendar);

    /// <summary>
    /// Calculates the Easter Sunday date for the specified year.
    /// </summary>
    /// <param name="year">The target year.</param>
    /// <returns>The resolved Easter Sunday date.</returns>
    protected abstract DateTime CalculateDate(int year);
}
