// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsFirstDayOfQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the current <see cref="DateOnly"/> instance represents the first day of its calendar quarter.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <returns><see langword="true"/> if the <paramref name="date"/> is the first day of its quarter; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This method evaluates whether the date component (ignoring the time of day) of the <paramref name="date"/> corresponds to the
    /// first day of the quarter based on the standard calendar year definition (January–March as Q1, etc.). The comparison is performed
    /// using the <see cref="DateOnly"/> property to normalize the time to midnight before evaluating the boundary.
    /// </remarks>
    public static bool IsFirstDayOfQuarter(this DateOnly date)
    {
        (int year, int quarter) = GetQuarterAndYearFromDate(CalendarQuarterDefinition.JanuaryToDecember, referenceDate: date);
        return date.DayNumber == ComputeQuarterStartDayNumber(year, quarter, GetQuarterDefinition(CalendarQuarterDefinition.JanuaryToDecember));
    }

    /// <summary>
    /// Determines whether the current <see cref="DateOnly"/> instance represents the first day of its calendar quarter based on a
    /// specified <see cref="CalendarQuarterDefinition"/>.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <param name="definition">Specifies the quarter definition used to determine the first date of the quarter.</param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="date"/> is the first day of its quarter based on the specified
    /// <paramref name="definition"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="definition"/> is not a valid member of the <see cref="CalendarQuarterDefinition"/> enumeration.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition"/> is <see cref="CalendarQuarterDefinition.Custom"/>. Use the
    /// <see cref="IsFirstDayOfQuarter(DateOnly, IQuarterDefinitionProvider)"/> overload with a custom
    /// <see cref="IQuarterDefinitionProvider"/> implementation in this case.
    /// </exception>
    /// <remarks>
    /// This method evaluates whether the date component (ignoring the time of day) of the <paramref name="date"/> corresponds to the
    /// first day of the quarter based on the provided <paramref name="definition"/>. The comparison is performed using the
    /// <see cref="DateOnly"/> property to normalize the time to midnight before evaluating the boundary.
    /// </remarks>
    public static bool IsFirstDayOfQuarter(this DateOnly date, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        (int year, int quarter) = GetQuarterAndYearFromDate(definition, referenceDate: date);
        return date.DayNumber == ComputeQuarterStartDayNumber(year, quarter, GetQuarterDefinition(definition));
    }

    /// <summary>
    /// Determines whether the current <see cref="DateOnly"/> instance represents the first day of its calendar quarter based on a
    /// custom quarter definition provided by an <see cref="IQuarterDefinitionProvider"/>.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <param name="provider">
    /// An implementation of <see cref="IQuarterDefinitionProvider"/> that defines how quarter boundaries are determined.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="date"/> is the first day of its quarter according to the rules of the specified
    /// <paramref name="provider"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method evaluates whether the date component (ignoring the time of day) of the <paramref name="date"/> corresponds to the
    /// first day of the quarter, using the logic defined by the specified <paramref name="provider"/>. The comparison is performed
    /// using the <see cref="DateOnly"/> property to normalize the time to midnight before evaluating the boundary.
    /// </remarks>
    public static bool IsFirstDayOfQuarter(this DateOnly date, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);
        return date.DayNumber == provider.GetQuarterStartDate(date).DayNumber;
    }
}