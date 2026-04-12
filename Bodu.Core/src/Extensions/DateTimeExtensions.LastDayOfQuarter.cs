// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.LastDayOfQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the last day of the specified calendar quarter in the given year, using the
    /// standard calendar quarter definition.
    /// </summary>
    /// <param name="year">The calendar year to evaluate. Must be between 1 and 9999, inclusive.</param>
    /// <param name="quarter">The quarter number, from 1 (Jan–Mar) to 4 (Oct–Dec).</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last day of the specified quarter and year.</returns>
    /// <remarks>
    /// <para>This method uses the standard calendar alignment defined by <see cref="CalendarQuarterDefinition.JanuaryToDecember" />.</para>
    /// <para>The <see cref="DateTime.Kind" /> property of the returned instance is <see cref="DateTimeKind.Unspecified" />.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" /> is outside the supported range,
    /// -or- if <paramref name="quarter" /> is less than 1 or greater than 4.
    /// </exception>
    public static DateTime GetLastDayOfQuarter(int year, int quarter) => GetLastDayOfQuarter(year, quarter, CalendarQuarterDefinition.JanuaryToDecember);

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the last day of the specified quarter and year, using the provided quarter definition.
    /// </summary>
    /// <param name="year">The calendar year to evaluate. Must be between 1 and 9999, inclusive.</param>
    /// <param name="quarter">The quarter number, from 1 to 4.</param>
    /// <param name="definition">The <see cref="CalendarQuarterDefinition" /> that defines how quarters are aligned.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last day of the specified quarter.</returns>
    /// <remarks>
    /// <para>The <see cref="DateTime.Kind" /> property of the returned instance is <see cref="DateTimeKind.Unspecified" />.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" /> is outside the supported range,
    /// -or- if <paramref name="quarter" /> is less than 1 or greater than 4,
    /// -or- if <paramref name="definition" /> is not a valid enumeration value.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition" /> is <see cref="CalendarQuarterDefinition.Custom" />. Use a provider-based overload instead.
    /// </exception>
    public static DateTime GetLastDayOfQuarter(int year, int quarter, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        return new DateTime(ComputeQuarterEndTicks(year, quarter, GetQuarterDefinition(definition)), DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the last day of the calendar quarter that contains the specified instance, using
    /// the standard calendar quarter definition.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the calendar quarter.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last day of the quarter containing <paramref name="dateTime" />.</returns>
    /// <remarks>
    /// <para>This method uses the standard calendar alignment defined by <see cref="CalendarQuarterDefinition.JanuaryToDecember" />.</para>
    /// <para>The <see cref="DateTime.Kind" /> property of the returned instance matches that of the original <paramref name="dateTime" />.</para>
    /// </remarks>
    public static DateTime LastDayOfQuarter(this DateTime dateTime) => LastDayOfQuarterInternal(dateTime, CalendarQuarterDefinition.JanuaryToDecember);

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the last day of the quarter that contains the specified instance, using the
    /// specified calendar quarter definition.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the quarter.</param>
    /// <param name="definition">The <see cref="CalendarQuarterDefinition" /> used to determine quarter boundaries.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last day of the corresponding quarter.</returns>
    /// <remarks>
    /// <para>
    /// The <paramref name="definition" /> controls whether quarters are aligned by month (e.g., Jan–Mar) or anchored to custom day-based boundaries.
    /// </para>
    /// <para>The <see cref="DateTime.Kind" /> property of the returned instance matches that of the original <paramref name="dateTime" />.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="definition" /> is not a valid enumeration value.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition" /> is <see cref="CalendarQuarterDefinition.Custom" />. Use the provider-based overload instead.
    /// </exception>
    public static DateTime LastDayOfQuarter(this DateTime dateTime, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        return LastDayOfQuarterInternal(dateTime, definition);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the last day of the quarter that contains the specified instance, using a custom <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the quarter.</param>
    /// <param name="provider">An implementation of <see cref="IQuarterDefinitionProvider" /> defining custom quarter boundaries.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last day of the quarter containing <paramref name="dateTime" />.</returns>
    /// <remarks>
    /// <para>The <see cref="DateTime.Kind" /> property of the returned instance matches that of the original <paramref name="dateTime" />.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the provider returns a date outside the range of <see cref="DateTime.MinValue" /> and <see cref="DateTime.MaxValue" />.
    /// </exception>
    public static DateTime LastDayOfQuarter(this DateTime dateTime, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);
        return provider.GetQuarterEnd(dateTime);
    }

    /// <summary>
    /// Returns the last day of the quarter that includes the specified <paramref name="dateTime" />, using the provided quarter definition.
    /// </summary>
    /// <param name="dateTime">The date used to evaluate the quarter.</param>
    /// <param name="definition">The quarter definition, assumed to be valid and non-custom.</param>
    /// <returns>An object whose value is set to midnight on the last day of the applicable quarter, preserving the <see cref="DateTime.Kind" />.</returns>
    /// <remarks>This method skips validation and is intended for internal use in trusted contexts.</remarks>
    private static DateTime LastDayOfQuarterInternal(this DateTime dateTime, CalendarQuarterDefinition definition)
    {
        (int year, int quarter) = GetQuarterAndYearFromDate(definition, referenceDate: dateTime);
        return new DateTime(ComputeQuarterEndTicks(year, quarter, GetQuarterDefinition(definition)), dateTime.Kind);
    }
}
