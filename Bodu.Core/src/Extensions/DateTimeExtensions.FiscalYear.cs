// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.FiscalYear.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the fiscal year that contains the supplied <see cref="DateTime" /> under the supplied
    /// <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="dateTime">The date to identify.</param>
    /// <param name="provider">The provider that defines the fiscal year boundaries.</param>
    /// <returns>The fiscal year number under the provider's conventions.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static int FiscalYear(this DateTime dateTime, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        return provider.GetFiscalYear(dateTime);
    }

    /// <summary>
    /// Returns the first calendar day of the supplied fiscal year under the supplied
    /// <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="fiscalYear">The fiscal year whose start date is requested.</param>
    /// <param name="provider">The provider that defines the fiscal year boundaries.</param>
    /// <returns>A <see cref="DateTime" /> representing the first day of the fiscal year (midnight).</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static DateTime FirstDateOfFiscalYear(int fiscalYear, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        return provider.GetQuarterStart(1, fiscalYear);
    }

    /// <summary>
    /// Returns the last calendar day of the supplied fiscal year under the supplied
    /// <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="fiscalYear">The fiscal year whose end date is requested.</param>
    /// <param name="provider">The provider that defines the fiscal year boundaries.</param>
    /// <returns>A <see cref="DateTime" /> representing the last day of the fiscal year (midnight).</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static DateTime LastDateOfFiscalYear(int fiscalYear, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        return provider.GetQuarterEnd(4, fiscalYear);
    }

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="dateTime" />'s date component is the first day of its
    /// fiscal year under the supplied <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="dateTime">The value to test.</param>
    /// <param name="provider">The provider that defines the fiscal year boundaries.</param>
    /// <returns>
    /// <see langword="true" /> if the date component equals the first day of its fiscal year; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static bool IsFirstDateOfFiscalYear(this DateTime dateTime, IQuarterDefinitionProvider provider) =>
        dateTime.Date == FirstDateOfFiscalYear(dateTime.FiscalYear(provider), provider).Date;

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="dateTime" />'s date component is the last day of its fiscal
    /// year under the supplied <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="dateTime">The value to test.</param>
    /// <param name="provider">The provider that defines the fiscal year boundaries.</param>
    /// <returns>
    /// <see langword="true" /> if the date component equals the last day of its fiscal year; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static bool IsLastDateOfFiscalYear(this DateTime dateTime, IQuarterDefinitionProvider provider) =>
        dateTime.Date == LastDateOfFiscalYear(dateTime.FiscalYear(provider), provider).Date;

    /// <summary>
    /// Returns a new <see cref="DateTime" /> obtained by advancing or retreating <paramref name="dateTime" /> by the
    /// signed number of fiscal years specified in <paramref name="count" />, preserving the time-of-day and
    /// <see cref="DateTime.Kind" />.
    /// </summary>
    /// <param name="dateTime">The starting date and time.</param>
    /// <param name="count">The signed number of fiscal years to apply.</param>
    /// <param name="provider">The provider that defines the fiscal year boundaries.</param>
    /// <returns>The date/time offset by <paramref name="count" /> fiscal years.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static DateTime AddFiscalYears(this DateTime dateTime, int count, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        if (count == 0) return new DateTime(dateTime.Ticks, dateTime.Kind);

        DateOnly result = DateOnly.FromDateTime(dateTime).AddFiscalYears(count, provider);
        return result.ToDateTime(TimeOnly.FromTimeSpan(dateTime.TimeOfDay), dateTime.Kind);
    }
}
