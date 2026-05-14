// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsLastDateOfQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateTime"/> falls on the last day of its calendar quarter, using the standard calendar quarter definition.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns><see langword="true"/> if <paramref name="dateTime"/> represents the last day of its quarter; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>This overload uses the standard calendar alignment defined by <see cref="CalendarQuarterDefinition.JanuaryToDecember"/>: Q1 = Jan – Mar, Q2 = Apr – Jun, Q3 = Jul – Sep, Q4 = Oct – Dec.</para>
    /// <para>The comparison is performed on the date component only; the time component of <paramref name="dateTime"/> is ignored.</para>
    /// </remarks>
    public static bool IsLastDateOfQuarter(this DateTime dateTime)
    {
        (var year, var quarter) = GetQuarterAndYearFromDate(CalendarQuarterDefinition.JanuaryToDecember, referenceDate: dateTime);
        return dateTime.Date.Ticks == ComputeQuarterEndTicks(year, quarter, GetQuarterDefinition(CalendarQuarterDefinition.JanuaryToDecember));
    }

    /// <summary>
    /// Determines whether the specified <see cref="DateTime"/> falls on the last day of its calendar quarter, using the supplied calendar quarter definition.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="definition">The <see cref="CalendarQuarterDefinition"/> that determines how quarter boundaries are aligned.</param>
    /// <returns><see langword="true"/> if <paramref name="dateTime"/> represents the last day of its quarter under <paramref name="definition"/>; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>The comparison is performed on the date component only; the time component of <paramref name="dateTime"/> is ignored.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="definition"/> is not a defined value of the <see cref="CalendarQuarterDefinition"/> enumeration.</exception>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="definition"/> is <see cref="CalendarQuarterDefinition.Custom"/>; use the provider-based overload instead.</exception>
    public static bool IsLastDateOfQuarter(this DateTime dateTime, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        (var year, var quarter) = GetQuarterAndYearFromDate(definition, referenceDate: dateTime);
        return dateTime.Date.Ticks == ComputeQuarterEndTicks(year, quarter, GetQuarterDefinition(definition));
    }

    /// <summary>
    /// Determines whether the specified <see cref="DateTime"/> falls on the last day of its calendar quarter, using a custom <see cref="IQuarterDefinitionProvider"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="provider">The <see cref="IQuarterDefinitionProvider"/> that defines custom quarter boundaries. Must not be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="dateTime"/> represents the last day of its quarter as defined by <paramref name="provider"/>; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>The comparison is performed on the date component only; the time component of <paramref name="dateTime"/> is ignored.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider"/> is <see langword="null"/>.</exception>
    public static bool IsLastDateOfQuarter(this DateTime dateTime, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        return dateTime.Date.Ticks == provider.GetQuarterEnd(dateTime).Date.Ticks;
    }
}
