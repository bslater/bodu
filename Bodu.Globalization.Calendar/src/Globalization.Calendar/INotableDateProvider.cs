// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Defines a contract for computing the calendar date of a notable event (e.g., Easter, Lunar New Year) based on a given year and
/// calendar system.
/// </summary>
/// <remarks>
/// Implementations of <see cref="INotableDateProvider" /> provide algorithmic calculation of dates that cannot be represented by a
/// fixed day and month, often varying by year and calendar type.
/// </remarks>
public interface INotableDateProvider
{
    /// <summary>
    /// Gets the earliest supported year for the provider.
    /// </summary>
    int MinSupportedYear { get; }

    /// <summary>
    /// Gets the latest supported year for the provider.
    /// </summary>
    int MaxSupportedYear { get; }

    /// <summary>
    /// Returns a value indicating whether the specified <paramref name="year" /> is supported by the provider.
    /// </summary>
    /// <param name="year">The year to test.</param>
    /// <returns>
    /// <see langword="true" /> if the provider can resolve dates for the specified <paramref name="year" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    bool SupportsYear(int year);

    /// <summary>
    /// Resolves all notable dates produced by the provider for the specified <paramref name="year" />.
    /// </summary>
    /// <param name="year">The year for which notable dates should be resolved.</param>
    /// <param name="calendar">
    /// Optional. A calendar context associated with the resolved dates. Providers may reject unsupported calendar types.
    /// </param>
    /// <returns>A read-only list of resolved notable dates for the specified year.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="year" /> is less than 1.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the specified <paramref name="calendar" /> type is not supported by the provider.
    /// </exception>
    IReadOnlyList<NotableDate> GetDates(int year, SysGlobal.Calendar? calendar = null);
}