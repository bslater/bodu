// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.IsLastDayOfQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime"/> instance represents the last day of its calendar quarter, based
    /// on the standard January–December quarter definition.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> to evaluate.</param>
    /// <returns><see langword="true"/> if <paramref name="dateTime"/> is the final calendar day of its quarter; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method uses the <see cref="CalendarQuarterDefinition.JanuaryToDecember"/> convention: Q1 = Jan–Mar, Q2 = Apr–Jun, Q3 =
    /// Jul–Sep, Q4 = Oct–Dec.
    /// </para>
    /// <para>The comparison is performed on the date component only (time is normalised to 00:00:00).</para>
    /// </remarks>
    public static bool IsLastDayOfQuarter(this DateTime dateTime)
    {
        (int year, int quarter) = GetQuarterAndYearFromDate(CalendarQuarterDefinition.JanuaryToDecember, referenceDate: dateTime);
        return dateTime.Date.Ticks == ComputeQuarterEndTicks(year, quarter, GetQuarterDefinition(CalendarQuarterDefinition.JanuaryToDecember));
    }

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime"/> instance represents the last day of its calendar quarter, based
    /// on a specified <see cref="CalendarQuarterDefinition"/>.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> to evaluate.</param>
    /// <param name="definition">The <see cref="CalendarQuarterDefinition"/> used to determine quarter boundaries. Must not be <see cref="CalendarQuarterDefinition.Custom"/>.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="dateTime"/> is the final calendar day of its quarter based on
    /// <paramref name="definition"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="definition"/> is not a defined member of <see cref="CalendarQuarterDefinition"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition"/> is <see cref="CalendarQuarterDefinition.Custom"/>. Use the overload that accepts an
    /// <see cref="IQuarterDefinitionProvider"/> instead.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The method evaluates whether the date portion of <paramref name="dateTime"/> (normalised to 00:00:00) is the final day of its
    /// quarter according to the specified definition.
    /// </para>
    /// </remarks>
    public static bool IsLastDayOfQuarter(this DateTime dateTime, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        (int year, int quarter) = GetQuarterAndYearFromDate(definition, referenceDate: dateTime);
        return dateTime.Date.Ticks == ComputeQuarterEndTicks(year, quarter, GetQuarterDefinition(definition));
    }

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime"/> instance represents the last day of its calendar quarter, based
    /// on a custom quarter definition provider.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> to evaluate.</param>
    /// <param name="provider">An implementation of <see cref="IQuarterDefinitionProvider"/> that defines custom quarter boundary logic.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="dateTime"/> is the final calendar day of its quarter according to
    /// <paramref name="provider"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// The method compares the date portion (normalised to 00:00:00) of <paramref name="dateTime"/> with the end of quarter date returned
    /// by the specified <paramref name="provider"/>.
    /// </para>
    /// </remarks>
    public static bool IsLastDayOfQuarter(this DateTime dateTime, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        return dateTime.Date.Ticks == provider.GetQuarterEnd(dateTime).Date.Ticks;
    }
}
