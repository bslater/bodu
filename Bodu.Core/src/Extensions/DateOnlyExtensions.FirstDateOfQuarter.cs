// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.FirstDateOfQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the first day of the calendar quarter that contains the
    /// specified <paramref name="date" />, using the standard calendar quarter definition.
    /// </summary>
    /// <param name="date">The date value used to determine the containing quarter.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the first day of the quarter that contains <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the standard calendar alignment defined by
    /// <see cref="CalendarQuarterDefinition.JanuaryToDecember" />:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Q1</term>
    /// <description>
    /// January – March
    /// </description>
    /// </item>
    /// <item>
    /// <term>Q2</term>
    /// <description>
    /// April – June
    /// </description>
    /// </item>
    /// <item>
    /// <term>Q3</term>
    /// <description>
    /// July – September
    /// </description>
    /// </item>
    /// <item>
    /// <term>Q4</term>
    /// <description>
    /// October – December
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    public static DateOnly FirstDateOfQuarter(this DateOnly date) => FirstDateOfQuarter(date, CalendarQuarterDefinition.JanuaryToDecember);

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the first day of the quarter that contains the specified
    /// <paramref name="date" />, using the specified calendar quarter definition.
    /// </summary>
    /// <param name="date">The date value used to determine the containing quarter.</param>
    /// <param name="definition">
    /// The <see cref="CalendarQuarterDefinition" /> that determines how quarter boundaries are aligned.
    /// </param>
    /// <returns>A <see cref="DateOnly" /> value set to the first day of the corresponding quarter.</returns>
    /// <remarks>
    /// <para>
    /// The <paramref name="definition" /> controls whether quarters are aligned to the first day of a month (e.g.
    /// January – March) or anchored to a custom day-of-month boundary.
    /// </para>
    /// <para>
    /// For provider-driven (e.g. 4-4-5 fiscal) quarters, use the
    /// <see cref="FirstDateOfQuarter(DateOnly, IQuarterDefinitionProvider)" /> overload instead.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="definition" /> is not a defined value of the <see cref="CalendarQuarterDefinition" />
    /// enumeration.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition" /> is <see cref="CalendarQuarterDefinition.Custom" />; use the
    /// provider-based overload instead.
    /// </exception>
    public static DateOnly FirstDateOfQuarter(this DateOnly date, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Invalid_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        (var year, var quarter) = GetQuarterAndYearFromDate(definition, referenceDate: date);
        return DateOnly.FromDayNumber(ComputeQuarterStartDayNumber(year, quarter, GetQuarterDefinition(definition)));
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the first day of the quarter that contains the specified
    /// <paramref name="date" />, using a custom <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="date">The date value used to determine the containing quarter.</param>
    /// <param name="provider">
    /// The <see cref="IQuarterDefinitionProvider" /> that defines custom quarter boundaries. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the first day of the quarter containing <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload supports advanced or domain-specific quarter systems by delegating boundary logic to the supplied
    /// <paramref name="provider" /> — for example, 4-4-5 retail calendars or regional fiscal quarters.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the <paramref name="provider" /> returns a date outside the range of <see cref="DateOnly.MinValue" />
    /// and <see cref="DateOnly.MaxValue" />.
    /// </exception>
    public static DateOnly FirstDateOfQuarter(this DateOnly date, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);
        return provider.GetQuarterStartDate(date);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the first day of the specified calendar
    /// <paramref name="quarter" /> in the given <paramref name="year" />, using the standard calendar quarter
    /// definition.
    /// </summary>
    /// <param name="year">
    /// The calendar year of the result. Must be between the <c>Year</c> property values of
    /// <see cref="DateOnly.MinValue" /> and <see cref="DateOnly.MaxValue" />, inclusive.
    /// </param>
    /// <param name="quarter">The quarter number, from 1 (Jan – Mar) through 4 (Oct – Dec).</param>
    /// <returns>A <see cref="DateOnly" /> value set to the first day of the specified quarter and year.</returns>
    /// <remarks>
    /// <para>
    /// This overload uses the standard calendar alignment defined by
    /// <see cref="CalendarQuarterDefinition.JanuaryToDecember" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="quarter" /> is less than 1 or greater than 4.
    /// </exception>
    public static DateOnly GetFirstDateOfQuarter(int year, int quarter) => GetFirstDateOfQuarter(year, quarter, CalendarQuarterDefinition.JanuaryToDecember);

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the first day of the specified <paramref name="quarter" />
    /// and <paramref name="year" />, using the supplied calendar quarter definition.
    /// </summary>
    /// <param name="year">
    /// The calendar year of the result. Must be between the <c>Year</c> property values of
    /// <see cref="DateOnly.MinValue" /> and <see cref="DateOnly.MaxValue" />, inclusive. If the quarter begins in the
    /// next calendar year (based on the definition's anchor), the year will be incremented accordingly.
    /// </param>
    /// <param name="quarter">The quarter number, from 1 through 4.</param>
    /// <param name="definition">
    /// The <see cref="CalendarQuarterDefinition" /> that determines how quarters are aligned.
    /// </param>
    /// <returns>A <see cref="DateOnly" /> value set to the first day of the specified quarter.</returns>
    /// <remarks>
    /// <para>
    /// The <paramref name="definition" /> controls whether quarters are aligned to the first day of a month or anchored
    /// to a custom day-of-month boundary.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="quarter" /> is less than 1 or greater than 4, -or- <paramref name="definition" /> is
    /// not a defined value of the <see cref="CalendarQuarterDefinition" /> enumeration.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition" /> is <see cref="CalendarQuarterDefinition.Custom" />; use a
    /// provider-based overload instead.
    /// </exception>
    public static DateOnly GetFirstDateOfQuarter(int year, int quarter, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        return definition == CalendarQuarterDefinition.Custom
            ? throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Invalid_ProviderInterface, nameof(IQuarterDefinitionProvider)))
            : DateOnly.FromDayNumber(ComputeQuarterStartDayNumber(year, quarter, GetQuarterDefinition(definition)));
    }
}
