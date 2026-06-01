// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IQuarterDefinitionProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Defines a provider interface for custom quarter calculation logic based on <see cref="DateTime" /> or
/// <see cref="DateOnly" /> values.
/// </summary>
/// <remarks>
/// <para>
/// Implementations describe a recurring quarter rule — a fiscal or logical calendar definition that is independent of
/// any single year. Year-specific values, such as quarter start and end dates or whether a fiscal year contains a 53rd
/// week, are derived on demand from the <see cref="DateTime" /> or <see cref="DateOnly" /> input, or from an explicit
/// <c>fiscalYear</c> argument.
/// </para>
/// <para>
/// Example use cases include 4–4–5 calendars, academic terms, retail fiscal calendars, or historical reporting quarters
/// that do not align with calendar or standard financial quarters.
/// </para>
/// <para>
/// Methods that support this interface include:
/// </para>
/// <list type="bullet">
/// <item>
/// <see cref="DateTimeExtensions.Quarter(DateTime, IQuarterDefinitionProvider)" />
/// </item>
/// <item>
/// <see cref="DateTimeExtensions.FirstDateOfQuarter(DateTime, IQuarterDefinitionProvider)" />
/// </item>
/// <item>
/// <see cref="DateTimeExtensions.LastDateOfQuarter(DateTime, IQuarterDefinitionProvider)" />
/// </item>
/// <item>
/// <see cref="DateTimeExtensions.IsFirstDateOfQuarter(DateTime, IQuarterDefinitionProvider)" />
/// </item>
/// <item>
/// <see cref="DateTimeExtensions.IsLastDateOfQuarter(DateTime, IQuarterDefinitionProvider)" />
/// </item>
/// <item>
/// <see cref="DateOnlyExtensions.Quarter(DateOnly, IQuarterDefinitionProvider)" />
/// </item>
/// <item>
/// <see cref="DateOnlyExtensions.FirstDateOfQuarter(DateOnly, IQuarterDefinitionProvider)" />
/// </item>
/// <item>
/// <see cref="DateOnlyExtensions.LastDateOfQuarter(DateOnly, IQuarterDefinitionProvider)" />
/// </item>
/// <item>
/// <see cref="DateOnlyExtensions.IsFirstDateOfQuarter(DateOnly, IQuarterDefinitionProvider)" />
/// </item>
/// <item>
/// <see cref="DateOnlyExtensions.IsLastDateOfQuarter(DateOnly, IQuarterDefinitionProvider)" />
/// </item>
/// </list>
/// </remarks>
public interface IQuarterDefinitionProvider
{
    /// <summary>
    /// Returns the quarter number (1–4) that contains the specified <see cref="DateTime" />.
    /// </summary>
    /// <param name="dateTime">The input <see cref="DateTime" /> for which to determine the quarter.</param>
    /// <returns>
    /// An integer in the range 1 to 4 representing the quarter that includes <paramref name="dateTime" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="dateTime" /> cannot be mapped to a recognized fiscal year.
    /// </exception>
    int GetQuarter(DateTime dateTime);

    /// <summary>
    /// Returns the quarter number (1–4) that contains the specified <see cref="DateOnly" />.
    /// </summary>
    /// <param name="dateOnly">The input <see cref="DateOnly" /> for which to determine the quarter.</param>
    /// <returns>
    /// An integer in the range 1 to 4 representing the quarter that includes <paramref name="dateOnly" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="dateOnly" /> cannot be mapped to a recognized fiscal year.
    /// </exception>
    int GetQuarter(DateOnly dateOnly);

    /// <summary>
    /// Returns the last day of the quarter that contains the specified <see cref="DateTime" />.
    /// </summary>
    /// <param name="dateTime">The input <see cref="DateTime" /> for which to determine the end of the quarter.</param>
    /// <returns>
    /// A <see cref="DateTime" /> set to midnight (00:00:00) on the final day of the quarter that includes
    /// <paramref name="dateTime" />. The result preserves the original <see cref="DateTime.Kind" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="dateTime" /> cannot be mapped to a recognized fiscal year.
    /// </exception>
    DateTime GetQuarterEnd(DateTime dateTime);

    /// <summary>
    /// Returns the last day of the specified quarter number (1–4).
    /// </summary>
    /// <param name="quarter">The quarter number (1–4).</param>
    /// <returns>
    /// A <see cref="DateTime" /> set to midnight (00:00:00) on the last day of the specified quarter.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="quarter" /> is not between 1 and 4.
    /// </exception>
    [Obsolete("Use the overload that accepts a fiscalYear parameter.")]
    DateTime GetQuarterEnd(int quarter);

    /// <summary>
    /// Returns the last day of the specified quarter number (1–4) within the given fiscal year.
    /// </summary>
    /// <param name="quarter">The quarter number (1–4).</param>
    /// <param name="fiscalYear">The fiscal year whose quarter boundary is being requested.</param>
    /// <returns>
    /// A <see cref="DateTime" /> set to midnight (00:00:00) on the last day of the specified quarter.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="quarter" /> is not between 1 and 4.
    /// </exception>
    DateTime GetQuarterEnd(int quarter, int fiscalYear);

    /// <summary>
    /// Returns the last day of the quarter that contains the specified <see cref="DateOnly" />.
    /// </summary>
    /// <param name="dateOnly">The input <see cref="DateOnly" /> for which to determine the end of the quarter.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> representing the final day of the quarter that includes <paramref name="dateOnly" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="dateOnly" /> cannot be mapped to a recognized fiscal year.
    /// </exception>
    DateOnly GetQuarterEndDate(DateOnly dateOnly);

    /// <summary>
    /// Returns the last day of the specified quarter number (1–4).
    /// </summary>
    /// <param name="quarter">The quarter number (1–4).</param>
    /// <returns>A <see cref="DateOnly" /> representing the final day of the specified quarter.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="quarter" /> is not between 1 and 4.
    /// </exception>
    [Obsolete("Use the overload that accepts a fiscalYear parameter.")]
    DateOnly GetQuarterEndDate(int quarter);

    /// <summary>
    /// Returns the last day of the specified quarter number (1–4) within the given fiscal year.
    /// </summary>
    /// <param name="quarter">The quarter number (1–4).</param>
    /// <param name="fiscalYear">The fiscal year whose quarter boundary is being requested.</param>
    /// <returns>A <see cref="DateOnly" /> representing the final day of the specified quarter.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="quarter" /> is not between 1 and 4.
    /// </exception>
    DateOnly GetQuarterEndDate(int quarter, int fiscalYear);

    /// <summary>
    /// Returns the first day of the quarter that contains the specified <see cref="DateTime" />.
    /// </summary>
    /// <param name="dateTime">
    /// The input <see cref="DateTime" /> for which to determine the start of the quarter.
    /// </param>
    /// <returns>
    /// A <see cref="DateTime" /> set to midnight (00:00:00) on the first day of the quarter that includes
    /// <paramref name="dateTime" />. The result preserves the original <see cref="DateTime.Kind" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="dateTime" /> cannot be mapped to a recognized fiscal year.
    /// </exception>
    DateTime GetQuarterStart(DateTime dateTime);

    /// <summary>
    /// Returns the first day of the specified quarter number (1–4).
    /// </summary>
    /// <param name="quarter">The quarter number (1–4).</param>
    /// <returns>
    /// A <see cref="DateTime" /> set to midnight (00:00:00) on the first day of the specified quarter.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="quarter" /> is not between 1 and 4.
    /// </exception>
    [Obsolete("Use the overload that accepts a fiscalYear parameter.")]
    DateTime GetQuarterStart(int quarter);

    /// <summary>
    /// Returns the first day of the specified quarter number (1–4) within the given fiscal year.
    /// </summary>
    /// <param name="quarter">The quarter number (1–4).</param>
    /// <param name="fiscalYear">The fiscal year whose quarter boundary is being requested.</param>
    /// <returns>
    /// A <see cref="DateTime" /> set to midnight (00:00:00) on the first day of the specified quarter.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="quarter" /> is not between 1 and 4.
    /// </exception>
    DateTime GetQuarterStart(int quarter, int fiscalYear);

    /// <summary>
    /// Returns the first day of the quarter that contains the specified <see cref="DateOnly" />.
    /// </summary>
    /// <param name="dateOnly">
    /// The input <see cref="DateOnly" /> for which to determine the start of the quarter.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> representing the first day of the quarter that includes <paramref name="dateOnly" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="dateOnly" /> cannot be mapped to a recognized fiscal year.
    /// </exception>
    DateOnly GetQuarterStartDate(DateOnly dateOnly);

    /// <summary>
    /// Returns the first day of the specified quarter number (1–4).
    /// </summary>
    /// <param name="quarter">The quarter number (1–4).</param>
    /// <returns>A <see cref="DateOnly" /> representing the first day of the specified quarter.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="quarter" /> is not between 1 and 4.
    /// </exception>
    [Obsolete("Use the overload that accepts a fiscalYear parameter.")]
    DateOnly GetQuarterStartDate(int quarter);

    /// <summary>
    /// Returns the first day of the specified quarter number (1–4) within the given fiscal year.
    /// </summary>
    /// <param name="quarter">The quarter number (1–4).</param>
    /// <param name="fiscalYear">The fiscal year whose quarter boundary is being requested.</param>
    /// <returns>A <see cref="DateOnly" /> representing the first day of the specified quarter.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="quarter" /> is not between 1 and 4.
    /// </exception>
    DateOnly GetQuarterStartDate(int quarter, int fiscalYear);

    /// <summary>
    /// Returns a value indicating whether the specified fiscal year contains 53 weeks rather than the standard 52.
    /// </summary>
    /// <param name="fiscalYear">The fiscal year to test.</param>
    /// <returns>
    /// <see langword="true" /> if the fiscal year spans 371 days (53 complete weeks); <see langword="false" /> if it
    /// spans 364 days (52 complete weeks).
    /// </returns>
    bool Is53WeekFiscalYear(int fiscalYear);

    /// <summary>
    /// Returns the total number of weeks in the specified fiscal year.
    /// </summary>
    /// <param name="fiscalYear">The fiscal year to query.</param>
    /// <returns>53 in a 53-week fiscal year; otherwise, 52.</returns>
    int GetWeeksInFiscalYear(int fiscalYear);

    /// <summary>
    /// Returns the fiscal year that contains the supplied <see cref="DateTime" />.
    /// </summary>
    /// <param name="dateTime">The date to identify.</param>
    /// <returns>The fiscal year number under the provider's conventions.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="dateTime" /> cannot be mapped to any fiscal year known to the provider.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The default implementation probes the calendar years <c>dateTime.Year - 1</c>, <c>dateTime.Year</c>, and
    /// <c>dateTime.Year + 1</c>, returning the candidate whose Q1 start and Q4 end bracket <paramref name="dateTime" />
    /// . This works for any provider whose fiscal year is contiguous and at most one calendar year away from the
    /// observed calendar year. Implementations may override with a more efficient or more precise computation.
    /// </para>
    /// </remarks>
    int GetFiscalYear(DateTime dateTime)
    {
        var calendarYear = dateTime.Year;
        for (var candidate = calendarYear - 1; candidate <= calendarYear + 1; candidate++)
        {
            DateTime q1Start = GetQuarterStart(1, candidate);
            DateTime q4End = GetQuarterEnd(4, candidate);
            if (dateTime >= q1Start && dateTime <= q4End)
                return candidate;
        }

        throw new ArgumentOutOfRangeException(
            nameof(dateTime),
            $"Unable to determine the fiscal year for date '{dateTime:yyyy-MM-dd}'.");
    }

    /// <summary>
    /// Returns the fiscal year that contains the supplied <see cref="DateOnly" />.
    /// </summary>
    /// <param name="dateOnly">The date to identify.</param>
    /// <returns>The fiscal year number under the provider's conventions.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="dateOnly" /> cannot be mapped to any fiscal year known to the provider.
    /// </exception>
    int GetFiscalYear(DateOnly dateOnly) => GetFiscalYear(dateOnly.ToDateTime(TimeOnly.MinValue));
}
