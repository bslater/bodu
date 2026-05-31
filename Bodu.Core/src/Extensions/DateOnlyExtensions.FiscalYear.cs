// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.FiscalYear.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the fiscal year that contains the supplied <see cref="DateOnly" /> under the supplied
    /// <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="date">The date to identify.</param>
    /// <param name="provider">The provider that defines the fiscal year boundaries.</param>
    /// <returns>The fiscal year number under the provider's conventions.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static int FiscalYear(this DateOnly date, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        return provider.GetFiscalYear(date);
    }

    /// <summary>
    /// Returns the first calendar day of the supplied fiscal year under the supplied
    /// <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="fiscalYear">The fiscal year whose start date is requested.</param>
    /// <param name="provider">The provider that defines the fiscal year boundaries.</param>
    /// <returns>A <see cref="DateOnly" /> representing the first day of the fiscal year.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static DateOnly FirstDateOfFiscalYear(int fiscalYear, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        return provider.GetQuarterStartDate(1, fiscalYear);
    }

    /// <summary>
    /// Returns the last calendar day of the supplied fiscal year under the supplied
    /// <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="fiscalYear">The fiscal year whose end date is requested.</param>
    /// <param name="provider">The provider that defines the fiscal year boundaries.</param>
    /// <returns>A <see cref="DateOnly" /> representing the last day of the fiscal year.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static DateOnly LastDateOfFiscalYear(int fiscalYear, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        return provider.GetQuarterEndDate(4, fiscalYear);
    }

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="date" /> is the first day of its fiscal year under the
    /// supplied <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="provider">The provider that defines the fiscal year boundaries.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="date" /> equals the first day of its fiscal year; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static bool IsFirstDateOfFiscalYear(this DateOnly date, IQuarterDefinitionProvider provider) =>
        date == FirstDateOfFiscalYear(date.FiscalYear(provider), provider);

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="date" /> is the last day of its fiscal year under the
    /// supplied <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="provider">The provider that defines the fiscal year boundaries.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="date" /> equals the last day of its fiscal year; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static bool IsLastDateOfFiscalYear(this DateOnly date, IQuarterDefinitionProvider provider) =>
        date == LastDateOfFiscalYear(date.FiscalYear(provider), provider);

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> obtained by advancing or retreating <paramref name="date" /> by the signed
    /// number of fiscal years specified in <paramref name="count" />.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="count">The signed number of fiscal years to apply.</param>
    /// <param name="provider">The provider that defines the fiscal year boundaries.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> whose value is offset by <paramref name="count" /> fiscal years, preserving the
    /// day-index within the fiscal year of <paramref name="date" />. The result is clamped to the last day of the
    /// target fiscal year when the source day-index does not fit (e.g. a Q4 53rd-week date mapped onto a 52-week year).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static DateOnly AddFiscalYears(this DateOnly date, int count, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        if (count == 0) return date;

        var fy = provider.GetFiscalYear(date);
        DateOnly fyStart = FirstDateOfFiscalYear(fy, provider);
        var dayIndex = date.DayNumber - fyStart.DayNumber;

        var target = fy + count;
        DateOnly targetStart = FirstDateOfFiscalYear(target, provider);
        DateOnly targetEnd = LastDateOfFiscalYear(target, provider);

        var candidate = targetStart.DayNumber + dayIndex;
        if (candidate > targetEnd.DayNumber) candidate = targetEnd.DayNumber;
        return DateOnly.FromDayNumber(candidate);
    }
}
