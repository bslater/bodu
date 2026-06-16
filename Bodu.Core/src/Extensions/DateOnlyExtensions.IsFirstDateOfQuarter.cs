// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsFirstDateOfQuarter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateOnly" /> falls on the first day of its calendar quarter, using
    /// the standard calendar quarter definition.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="date" /> represents the first day of its quarter; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the standard calendar alignment defined by
    /// <see cref="CalendarQuarterDefinition.JanuaryToDecember" />: Q1 = Jan – Mar, Q2 = Apr – Jun, Q3 = Jul – Sep, Q4 =
    /// Oct – Dec.
    /// </para>
    /// </remarks>
    public static bool IsFirstDateOfQuarter(this DateOnly date)
    {
        (int year, int quarter) = GetQuarterAndYearFromDate(CalendarQuarterDefinition.JanuaryToDecember, referenceDate: date);
        return date.DayNumber == ComputeQuarterStartDayNumber(year, quarter, GetQuarterDefinition(CalendarQuarterDefinition.JanuaryToDecember));
    }

    /// <summary>
    /// Determines whether the specified <see cref="DateOnly" /> falls on the first day of its calendar quarter, using
    /// the supplied calendar quarter definition.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="definition">
    /// The <see cref="CalendarQuarterDefinition" /> that determines how quarter boundaries are aligned.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="date" /> represents the first day of its quarter under
    /// <paramref name="definition" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="definition" /> is not a defined value of the <see cref="CalendarQuarterDefinition" />
    /// enumeration.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition" /> is <see cref="CalendarQuarterDefinition.Custom" />; use the
    /// provider-based overload instead.
    /// </exception>
    public static bool IsFirstDateOfQuarter(this DateOnly date, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);
        if (definition == CalendarQuarterDefinition.Custom) throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        (int year, int quarter) = GetQuarterAndYearFromDate(definition, referenceDate: date);
        return date.DayNumber == ComputeQuarterStartDayNumber(year, quarter, GetQuarterDefinition(definition));
    }

    /// <summary>
    /// Determines whether the specified <see cref="DateOnly" /> falls on the first day of its calendar quarter, using a
    /// custom <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="provider">
    /// The <see cref="IQuarterDefinitionProvider" /> that defines custom quarter boundaries. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="date" /> represents the first day of its quarter as defined by
    /// <paramref name="provider" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static bool IsFirstDateOfQuarter(this DateOnly date, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);
        return date.DayNumber == provider.GetQuarterStartDate(date).DayNumber;
    }
}
