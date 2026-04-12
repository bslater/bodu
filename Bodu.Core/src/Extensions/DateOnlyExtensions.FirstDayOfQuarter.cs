// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.FirstDayOfQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a <see cref="DateOnly" /> representing the first day of the calendar quarter for the specified <paramref name="date" />.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> whose quarter is evaluated.</param>
    /// <returns>A <see cref="DateOnly" /> value representing the first day of the quarter that includes <paramref name="date" />.</returns>
    /// <remarks>
    /// This method uses the standard calendar quarter definition:
    /// <list type="bullet">
    /// <item>
    /// <term>Q1</term>
    /// <description>January–March</description>
    /// </item>
    /// <item>
    /// <term>Q2</term>
    /// <description>April–June</description>
    /// </item>
    /// <item>
    /// <term>Q3</term>
    /// <description>July–September</description>
    /// </item>
    /// <item>
    /// <term>Q4</term>
    /// <description>October–December</description>
    /// </item>
    /// </list>
    /// </remarks>
    public static DateOnly FirstDayOfQuarter(this DateOnly date) => FirstDayOfQuarter(date, CalendarQuarterDefinition.JanuaryToDecember);

    /// <summary>
    /// Returns a <see cref="DateOnly" /> representing the first day of the specified <paramref name="quarter" /> in the given
    /// <paramref name="year" />, based on the standard calendar quarter definition.
    /// </summary>
    /// <param name="quarter">
    /// The quarter number to evaluate (1 through 4), using the conventional calendar-based quarters: Q1 = Jan–Mar, Q2 = Apr–Jun, Q3 =
    /// Jul–Sep, Q4 = Oct–Dec.
    /// </param>
    /// <param name="year">
    /// The calendar year in which the quarter is to be evaluated. The returned date will represent the start of the quarter within this year.
    /// </param>
    /// <returns>A <see cref="DateOnly" /> representing the first day of the specified calendar quarter in the given year.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="quarter" /> is not between 1 and 4 (inclusive).</exception>
    /// <remarks>
    /// <para>This method uses the standard month-aligned calendar quarter system defined by <see cref="CalendarQuarterDefinition.JanuaryToDecember" />:</para>
    /// <list type="bullet">
    /// <item>
    /// <term>Q1</term>
    /// <description>January – March</description>
    /// </item>
    /// <item>
    /// <term>Q2</term>
    /// <description>April – June</description>
    /// </item>
    /// <item>
    /// <term>Q3</term>
    /// <description>July – September</description>
    /// </item>
    /// <item>
    /// <term>Q4</term>
    /// <description>October – December</description>
    /// </item>
    /// </list>
    /// <para>
    /// For other fiscal or custom quarter definitions, use the <see cref="FirstDayOfQuarter(CalendarQuarterDefinition, int, int)" /> or
    /// provider-based overloads.
    /// </para>
    /// </remarks>
    public static DateOnly FirstDayOfQuarter(int quarter, int year) => FirstDayOfQuarter(CalendarQuarterDefinition.JanuaryToDecember, quarter, year);

    /// <summary>
    /// Returns the first day of the quarter that includes the specified <paramref name="date" />, based on the provided <see cref="CalendarQuarterDefinition" />.
    /// </summary>
    /// <param name="date">
    /// The <see cref="DateOnly" /> to evaluate. The returned value will be the first calendar day of the quarter that contains this
    /// date, according to the selected definition.
    /// </param>
    /// <param name="definition">Specifies the quarter definition used to determine the first date of the quarter.</param>
    /// <returns>A <see cref="DateOnly" /> representing the first day of the identified quarter.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="definition" /> is not a valid member of the <see cref="CalendarQuarterDefinition" /> enumeration.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition" /> is <see cref="CalendarQuarterDefinition.Custom" />, which requires a provider-based
    /// overload (see <see cref="FirstDayOfQuarter(DateOnly, IQuarterDefinitionProvider)" />).
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method supports both month-aligned and day-aligned quarter definitions. When the definition is not aligned to the first day
    /// of a month, the result will reflect the exact starting day of the quarter as defined.
    /// </para>
    /// <para>
    /// For custom or non-standard quarter systems, use the <see cref="FirstDayOfQuarter(DateOnly, IQuarterDefinitionProvider)" />
    /// overload with a custom implementation of <see cref="IQuarterDefinitionProvider" />.
    /// </para>
    /// </remarks>
    public static DateOnly FirstDayOfQuarter(this DateOnly date, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        (int year, int quarter) = GetQuarterAndYearFromDate(definition, referenceDate: date);
        return DateOnly.FromDayNumber(ComputeQuarterStartDayNumber(year, quarter, GetQuarterDefinition(definition)));
    }

    /// <summary>
    /// Returns the first day of the specified <paramref name="quarter" /> in the given <paramref name="year" />, based on the provided <see cref="CalendarQuarterDefinition" />.
    /// </summary>
    /// <param name="definition">Specifies the quarter definition used to determine the first date of the quarter.</param>
    /// <param name="quarter">
    /// The quarter number to evaluate (1 through 4). Represents the Nth quarter following the definition’s anchor month and day.
    /// </param>
    /// <param name="year">
    /// The reference calendar year. The result will represent the start date of the specified quarter relative to this year. If the
    /// quarter begins in the next calendar year (based on modular month arithmetic), the year will be incremented accordingly.
    /// </param>
    /// <returns>A <see cref="DateOnly" /> representing the first day of the specified quarter in the applicable year.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="quarter" /> is not between 1 and 4 (inclusive), or if <paramref name="definition" /> is not a defined
    /// value of the <see cref="CalendarQuarterDefinition" /> enumeration.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition" /> is <see cref="CalendarQuarterDefinition.Custom" />, which requires a provider-based
    /// overload. Use the <see cref="FirstDayOfQuarter(DateOnly, IQuarterDefinitionProvider)" /> method instead.
    /// </exception>
    /// <remarks>
    /// <para>
    /// For month-aligned definitions (e.g., <c>07</c> for July), each quarter starts on the 1st day of the corresponding month and
    /// repeats every 3 months.
    /// </para>
    /// <para>
    /// For day-aligned definitions (e.g., <c>0406</c> for April 6), each quarter starts on a specific day-of-month, and subsequent
    /// quarters maintain the day anchor across three-month intervals.
    /// </para>
    /// </remarks>
    public static DateOnly FirstDayOfQuarter(CalendarQuarterDefinition definition, int quarter, int year)
    {
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        return DateOnly.FromDayNumber(ComputeQuarterStartDayNumber(year, quarter, GetQuarterDefinition(definition)));
    }

    /// <summary>
    /// Returns the first day of the quarter based on a custom <see cref="IQuarterDefinitionProvider" /> implementation.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value whose quarter is being evaluated.</param>
    /// <param name="provider">The <see cref="IQuarterDefinitionProvider" /> that defines custom quarter logic.</param>
    /// <returns>A <see cref="DateOnly" /> representing the first calendar day of the applicable custom quarter.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="provider" /> returns an invalid quarter boundary.</exception>
    /// <remarks>
    /// This method supports advanced or domain-specific definitions of quarters by delegating logic to the specified
    /// <paramref name="provider" />, such as 4-4-5 fiscal calendars or regional fiscal systems.
    /// </remarks>
    public static DateOnly FirstDayOfQuarter(this DateOnly date, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);
        return provider.GetQuarterStartDate(date);
    }
}