// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.LastDateOfQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the last day of the calendar quarter that contains the specified <paramref name="dateTime"/>, using the standard calendar quarter definition.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing quarter.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last day of the quarter that contains <paramref name="dateTime"/>, with the original <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>This overload uses the standard calendar alignment defined by <see cref="CalendarQuarterDefinition.JanuaryToDecember"/>:</para>
    /// <list type="bullet">
    /// <item><term>Q1</term><description>January – March</description></item>
    /// <item><term>Q2</term><description>April – June</description></item>
    /// <item><term>Q3</term><description>July – September</description></item>
    /// <item><term>Q4</term><description>October – December</description></item>
    /// </list>
    /// <para>The returned value has its time component normalized to midnight (00:00:00), and the original <see cref="DateTime.Kind"/> is retained.</para>
    /// </remarks>
    public static DateTime LastDateOfQuarter(this DateTime dateTime) => LastDateOfQuarterInternal(dateTime, CalendarQuarterDefinition.JanuaryToDecember);

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the last day of the quarter that contains the specified <paramref name="dateTime"/>, using the specified calendar quarter definition.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing quarter.</param>
    /// <param name="definition">The <see cref="CalendarQuarterDefinition"/> that determines how quarter boundaries are aligned.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last day of the corresponding quarter, with the original <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>The <paramref name="definition"/> controls whether quarters are aligned to the first day of a month (e.g. January – March) or anchored to a custom day-of-month boundary.</para>
    /// <para>For provider-driven (e.g. 4-4-5 fiscal) quarters, use the <see cref="LastDateOfQuarter(DateTime, IQuarterDefinitionProvider)"/> overload instead.</para>
    /// <para>The returned value has its time component normalized to midnight (00:00:00), and the original <see cref="DateTime.Kind"/> is retained.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="definition"/> is not a defined value of the <see cref="CalendarQuarterDefinition"/> enumeration.</exception>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="definition"/> is <see cref="CalendarQuarterDefinition.Custom"/>; use the provider-based overload instead.</exception>
    public static DateTime LastDateOfQuarter(this DateTime dateTime, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        return LastDateOfQuarterInternal(dateTime, definition);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the last day of the quarter that contains the specified <paramref name="dateTime"/>, using a custom <see cref="IQuarterDefinitionProvider"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing quarter.</param>
    /// <param name="provider">The <see cref="IQuarterDefinitionProvider"/> that defines custom quarter boundaries. Must not be <see langword="null"/>.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last day of the quarter containing <paramref name="dateTime"/>, with the original <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>This overload supports advanced or domain-specific quarter systems by delegating boundary logic to the supplied <paramref name="provider"/> — for example, 4-4-5 retail calendars or regional fiscal quarters.</para>
    /// <para>The returned value has its time component normalized to midnight (00:00:00), and the original <see cref="DateTime.Kind"/> is retained.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="provider"/> returns a date outside the range of <see cref="DateTime.MinValue"/> and <see cref="DateTime.MaxValue"/>.</exception>
    public static DateTime LastDateOfQuarter(this DateTime dateTime, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);
        return provider.GetQuarterEnd(dateTime);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the last day of the specified calendar <paramref name="quarter"/> in the given <paramref name="year"/>, using the standard calendar quarter definition.
    /// </summary>
    /// <param name="year">The calendar year of the result. Must be between the <c>Year</c> property values of <see cref="DateTime.MinValue"/> and <see cref="DateTime.MaxValue"/>, inclusive.</param>
    /// <param name="quarter">The quarter number, from 1 (Jan – Mar) through 4 (Oct – Dec).</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last day of the specified quarter and year, using <see cref="DateTimeKind.Unspecified"/>.</returns>
    /// <remarks>
    /// <para>This overload uses the standard calendar alignment defined by <see cref="CalendarQuarterDefinition.JanuaryToDecember"/>.</para>
    /// <para>The returned value is normalized to midnight (00:00:00) and uses <see cref="DateTimeKind.Unspecified"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year"/> is less than the <c>Year</c> of <see cref="DateTime.MinValue"/> or greater than that of <see cref="DateTime.MaxValue"/>,
    /// -or- <paramref name="quarter"/> is less than 1 or greater than 4.
    /// </exception>
    public static DateTime GetLastDateOfQuarter(int year, int quarter) => GetLastDateOfQuarter(year, quarter, CalendarQuarterDefinition.JanuaryToDecember);

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the last day of the specified <paramref name="quarter"/> and <paramref name="year"/>, using the supplied calendar quarter definition.
    /// </summary>
    /// <param name="year">The calendar year of the result. Must be between the <c>Year</c> property values of <see cref="DateTime.MinValue"/> and <see cref="DateTime.MaxValue"/>, inclusive.</param>
    /// <param name="quarter">The quarter number, from 1 through 4.</param>
    /// <param name="definition">The <see cref="CalendarQuarterDefinition"/> that determines how quarters are aligned.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last day of the specified quarter, using <see cref="DateTimeKind.Unspecified"/>.</returns>
    /// <remarks>
    /// <para>The <paramref name="definition"/> controls whether quarters are aligned to the first day of a month or anchored to a custom day-of-month boundary.</para>
    /// <para>The returned value is normalized to midnight (00:00:00) and uses <see cref="DateTimeKind.Unspecified"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="quarter"/> is less than 1 or greater than 4,
    /// -or- <paramref name="definition"/> is not a defined value of the <see cref="CalendarQuarterDefinition"/> enumeration.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="definition"/> is <see cref="CalendarQuarterDefinition.Custom"/>; use a provider-based overload instead.</exception>
    public static DateTime GetLastDateOfQuarter(int year, int quarter, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        return new DateTime(ComputeQuarterEndTicks(year, quarter, GetQuarterDefinition(definition)), DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Returns the last day of the quarter that contains the specified <paramref name="dateTime"/>, using a prevalidated calendar quarter <paramref name="definition"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing quarter.</param>
    /// <param name="definition">A defined <see cref="CalendarQuarterDefinition"/> that is not <see cref="CalendarQuarterDefinition.Custom"/>.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last day of the quarter, with the original <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>This helper performs no validation and is intended for internal use where the <paramref name="definition"/> is known to be valid.</remarks>
    private static DateTime LastDateOfQuarterInternal(this DateTime dateTime, CalendarQuarterDefinition definition)
    {
        (var year, var quarter) = GetQuarterAndYearFromDate(definition, referenceDate: dateTime);
        return new DateTime(ComputeQuarterEndTicks(year, quarter, GetQuarterDefinition(definition)), dateTime.Kind);
    }
}
