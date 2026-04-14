// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.LastDayOfQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a <see cref="DateOnly"/> representing the last day of the calendar quarter for the specified <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly"/> whose quarter is evaluated.</param>
    /// <returns>A <see cref="DateOnly"/> value representing the last day of the quarter that includes <paramref name="date"/>.</returns>
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
    public static DateOnly LastDayOfQuarter(this DateOnly date) => LastDayOfQuarter(date, CalendarQuarterDefinition.JanuaryToDecember);

    /// <summary>
    /// Returns a <see cref="DateOnly"/> representing the last day of the specified <paramref name="quarter"/> in the given
    /// <paramref name="year"/>, using the standard month-aligned calendar quarter definition.
    /// </summary>
    /// <param name="quarter">
    /// The quarter number to evaluate (1 through 4), based on the conventional calendar quarters: Q1 = January–March, Q2 = April–June,
    /// Q3 = July–September, Q4 = October–December.
    /// </param>
    /// <param name="year">
    /// The calendar year used to evaluate the quarter. The returned date will represent the last day of the quarter relative to this year.
    /// </param>
    /// <returns>A <see cref="DateOnly"/> representing the final day of the specified calendar quarter in the given year.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="quarter"/> is not between 1 and 4 (inclusive).</exception>
    /// <remarks>
    /// <para>This method uses the <see cref="CalendarQuarterDefinition.JanuaryToDecember"/> structure as the default quarter system:</para>
    /// <list type="bullet">
    /// <item>
    /// <term>Q1</term>
    /// <description>January 1 – March 31</description>
    /// </item>
    /// <item>
    /// <term>Q2</term>
    /// <description>April 1 – June 30</description>
    /// </item>
    /// <item>
    /// <term>Q3</term>
    /// <description>July 1 – September 30</description>
    /// </item>
    /// <item>
    /// <term>Q4</term>
    /// <description>October 1 – December 31</description>
    /// </item>
    /// </list>
    /// <para>
    /// To evaluate alternate fiscal or day-aligned definitions, use the
    /// <see cref="LastDayOfQuarter(CalendarQuarterDefinition, int, int)"/> or provider-based overloads.
    /// </para>
    /// </remarks>
    public static DateOnly LastDayOfQuarter(int quarter, int year) => LastDayOfQuarter(CalendarQuarterDefinition.JanuaryToDecember, quarter, year);

    /// <summary>
    /// Returns the last day of the quarter that includes the specified <paramref name="date"/>, based on the provided <see cref="CalendarQuarterDefinition"/>.
    /// </summary>
    /// <param name="date">
    /// The <see cref="DateOnly"/> to evaluate. The returned value will be the final calendar day of the quarter that contains this
    /// date, as defined by the selected quarter structure.
    /// </param>
    /// <param name="definition">Specifies the quarter definition used to determine the last date of the quarter.</param>
    /// <returns>A <see cref="DateOnly"/> representing the last day of the determined quarter.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="definition"/> is not a valid member of the <see cref="CalendarQuarterDefinition"/> enumeration.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition"/> is <see cref="CalendarQuarterDefinition.Custom"/>. Use the
    /// <see cref="LastDayOfQuarter(DateOnly, IQuarterDefinitionProvider)"/> overload with a custom
    /// <see cref="IQuarterDefinitionProvider"/> implementation in this case.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method supports both month-aligned and day-aligned quarter structures. When the definition does not align to the first day
    /// of a month, the result is computed based on the anchor day and adjusted accordingly.
    /// </para>
    /// <para>For non-standard quarter systems (e.g., 4-4-5 or 13-week models), use the provider-based overload <see cref="LastDayOfQuarter(DateOnly, IQuarterDefinitionProvider)"/>.</para>
    /// </remarks>
    public static DateOnly LastDayOfQuarter(this DateOnly date, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        (int year, int quarter) = GetQuarterAndYearFromDate(definition, referenceDate: date);
        return DateOnly.FromDayNumber(ComputeQuarterEndDayNumber(year, quarter, GetQuarterDefinition(definition)));
    }

    /// <summary>
    /// Returns the last day of the specified <paramref name="quarter"/> in the given <paramref name="year"/>, based on the provided <see cref="CalendarQuarterDefinition"/>.
    /// </summary>
    /// <param name="definition">Specifies the quarter definition used to determine the last date of the quarter.</param>
    /// <param name="quarter">
    /// The quarter number to evaluate (1 through 4). Represents the Nth quarter following the definition’s anchor month and day.
    /// </param>
    /// <param name="year">
    /// The reference calendar year. The result will represent the end date of the specified quarter relative to this year. If the
    /// quarter ends in the next calendar year (based on modular month arithmetic), the year will be incremented accordingly.
    /// </param>
    /// <returns>A <see cref="DateOnly"/> representing the last day of the specified quarter in the applicable year.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="quarter"/> is not between 1 and 4 (inclusive), or if <paramref name="definition"/> is not a defined
    /// value of the <see cref="CalendarQuarterDefinition"/> enumeration.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition"/> is <see cref="CalendarQuarterDefinition.Custom"/>, which requires a provider-based
    /// overload. Use the <see cref="LastDayOfQuarter(DateOnly, IQuarterDefinitionProvider)"/> method instead.
    /// </exception>
    /// <remarks>
    /// <para>For month-aligned definitions (e.g., <c>07</c> for July), each quarter ends on the last day of the third month.</para>
    /// <para>
    /// For day-aligned definitions (e.g., <c>0406</c> for April 6), each quarter ends the day before the next quarter’s anchor day.
    /// </para>
    /// </remarks>
    public static DateOnly LastDayOfQuarter(CalendarQuarterDefinition definition, int quarter, int year)
    {
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        return DateOnly.FromDayNumber(ComputeQuarterEndDayNumber(year, quarter, GetQuarterDefinition(definition)));
    }

    /// <summary>
    /// Returns the last day of the quarter based on a custom <see cref="IQuarterDefinitionProvider"/> implementation.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly"/> value whose quarter is being evaluated.</param>
    /// <param name="provider">The <see cref="IQuarterDefinitionProvider"/> that defines custom quarter logic.</param>
    /// <returns>A <see cref="DateOnly"/> representing the last calendar day of the applicable custom quarter.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="provider"/> returns an invalid quarter boundary.</exception>
    /// <remarks>
    /// This method supports advanced quarter systems such as 4-4-5 accounting or domain-specific fiscal quarters by delegating logic to
    /// the specified <paramref name="provider"/>.
    /// </remarks>
    public static DateOnly LastDayOfQuarter(this DateOnly date, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);
        return provider.GetQuarterEndDate(date);
    }
}