// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.IsFirstDayOfQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is the first day of its calendar quarter, based on the standard
    /// calendar quarter definition (Q1: January–March, Q2: April–June, etc.).
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns><see langword="true" /> if <paramref name="dateTime" /> is the first day of its quarter; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// <para>
    /// This method uses <see cref="CalendarQuarterDefinition.JanuaryToDecember" /> to determine quarter boundaries. The result is computed
    /// by comparing the date component of <paramref name="dateTime" /> against the start of its containing quarter.
    /// </para>
    /// </remarks>
    public static bool IsFirstDayOfQuarter(this DateTime dateTime)
    {
        (int year, int quarter) = GetQuarterAndYearFromDate(CalendarQuarterDefinition.JanuaryToDecember, referenceDate: dateTime);
        return dateTime.Date.Ticks == ComputeQuarterStartTicks(year, quarter, GetQuarterDefinition(CalendarQuarterDefinition.JanuaryToDecember));
    }

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is the first day of its calendar quarter, based on the specified <see cref="CalendarQuarterDefinition" />.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="definition">The quarter definition used to determine the start of the quarter (e.g., <see cref="CalendarQuarterDefinition.AprilToMarch" />).</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="dateTime" /> is the first day of its quarter based on the given definition; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method compares the date component of <paramref name="dateTime" /> to the computed start of the quarter, using the provided <paramref name="definition" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="definition" /> is not a valid <see cref="CalendarQuarterDefinition" /> value.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition" /> is <see cref="CalendarQuarterDefinition.Custom" />. Use
    /// <see cref="IsFirstDayOfQuarter(DateTime, IQuarterDefinitionProvider)" /> for custom quarter logic.
    /// </exception>
    public static bool IsFirstDayOfQuarter(this DateTime dateTime, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        (int year, int quarter) = GetQuarterAndYearFromDate(definition, referenceDate: dateTime);
        return dateTime.Date.Ticks == ComputeQuarterStartTicks(year, quarter, GetQuarterDefinition(definition));
    }

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is the first day of its calendar quarter, based on a custom <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="provider">A custom quarter provider that defines the start of each quarter.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="dateTime" /> is the first day of its quarter as defined by the provider; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method compares the date component of <paramref name="dateTime" /> to the result returned by <paramref name="provider" />.
    /// Time-of-day is ignored in the comparison.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider" /> is <see langword="null" />.</exception>
    public static bool IsFirstDayOfQuarter(this DateTime dateTime, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        return dateTime.Date.Ticks == provider.GetQuarterStart(dateTime).Date.Ticks;
    }
}
