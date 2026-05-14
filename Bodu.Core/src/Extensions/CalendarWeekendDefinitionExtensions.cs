// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarWeekendDefinitionExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides conversions between the legacy <see cref="CalendarWeekendDefinition" /> enum (plus optional
/// <see cref="IWeekendDefinitionProvider" />) and the canonical <see cref="WeekPattern" /> working-week value type.
/// </summary>
public static class CalendarWeekendDefinitionExtensions
{
    /// <summary>
    /// Converts a <see cref="CalendarWeekendDefinition" /> (plus an optional <see cref="IWeekendDefinitionProvider" />
    /// for <see cref="CalendarWeekendDefinition.Custom" />) into the canonical <see cref="WeekPattern" /> whose
    /// selected days are the working days implied by the supplied weekend definition.
    /// </summary>
    /// <param name="weekend">The weekend definition.</param>
    /// <param name="provider">An optional custom weekend provider consulted when <paramref name="weekend" /> is <see cref="CalendarWeekendDefinition.Custom" />.</param>
    /// <returns>The working-week <see cref="WeekPattern" /> implied by <paramref name="weekend" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="weekend" /> is not a defined value of the <see cref="CalendarWeekendDefinition" />
    /// enumeration, or when <paramref name="weekend" /> is <see cref="CalendarWeekendDefinition.Custom" /> and
    /// <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The conversion enumerates every <see cref="DayOfWeek" /> value and queries
    /// <see cref="DateTimeExtensions.IsWeekend(DayOfWeek, CalendarWeekendDefinition, IWeekendDefinitionProvider?)" />,
    /// selecting each day that is not classified as a weekend. <see cref="CalendarWeekendDefinition.None" /> therefore
    /// produces a <see cref="WeekPattern" /> with every day selected.
    /// </para>
    /// </remarks>
    public static WeekPattern ToWeekPattern(this CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider = null)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);
        ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull(provider, weekend, CalendarWeekendDefinition.Custom);

        WeekPattern pattern = WeekPattern.Empty;
        for (var i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)i;
            if (!DateTimeExtensions.IsWeekend(day, weekend, provider))
                pattern = pattern.With(day);
        }

        return pattern;
    }

    /// <summary>
    /// Returns the matching <see cref="CalendarWeekendDefinition" /> for the supplied working-week
    /// <see cref="WeekPattern" />, or <see cref="CalendarWeekendDefinition.Custom" /> when no built-in value matches.
    /// </summary>
    /// <param name="workingWeek">The working-week pattern.</param>
    /// <returns>
    /// The matching <see cref="CalendarWeekendDefinition" /> value (for example
    /// <see cref="CalendarWeekendDefinition.SaturdaySunday" /> when <paramref name="workingWeek" /> is Monday through
    /// Friday), or <see cref="CalendarWeekendDefinition.Custom" /> when <paramref name="workingWeek" /> does not match
    /// any built-in weekend definition.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Callers that need to distinguish "no match" from "Custom" should use
    /// <see cref="TryGetCalendarWeekendDefinition(WeekPattern, out CalendarWeekendDefinition)" />.
    /// </para>
    /// </remarks>
    public static CalendarWeekendDefinition ToCalendarWeekendDefinition(this WeekPattern workingWeek) =>
        TryGetCalendarWeekendDefinition(workingWeek, out CalendarWeekendDefinition value)
            ? value
            : CalendarWeekendDefinition.Custom;

    /// <summary>
    /// Attempts to identify the supplied <see cref="WeekPattern" /> as a built-in
    /// <see cref="CalendarWeekendDefinition" /> value.
    /// </summary>
    /// <param name="workingWeek">The working-week pattern to identify.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, contains the matching
    /// <see cref="CalendarWeekendDefinition" /> value; otherwise <see cref="CalendarWeekendDefinition.Custom" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="workingWeek" /> corresponds to a built-in weekend definition;
    /// otherwise <see langword="false" />.
    /// </returns>
    public static bool TryGetCalendarWeekendDefinition(
        this WeekPattern workingWeek,
        out CalendarWeekendDefinition value)
    {
        (var matched, value) = workingWeek switch
        {
            var week when week == WeekPattern.MondayToFriday =>
                (true, CalendarWeekendDefinition.SaturdaySunday),

            var week when week == WeekPattern.SundayToThursday =>
                (true, CalendarWeekendDefinition.FridaySaturday),

            var week when week == WeekPattern.SaturdayToWednesday =>
                (true, CalendarWeekendDefinition.ThursdayFriday),

            var week when week == WeekPattern.SaturdayToThursday =>
                (true, CalendarWeekendDefinition.FridayOnly),

            var week when week == WeekPattern.MondayToSaturday =>
                (true, CalendarWeekendDefinition.SundayOnly),

            var week when week.Count == 7 =>
                (true, CalendarWeekendDefinition.None),

            _ =>
                (false, CalendarWeekendDefinition.Custom),
        };

        return matched;
    }

    /// <summary>
    /// Converts an <see cref="IWeekendDefinitionProvider" /> into the canonical <see cref="WeekPattern" /> whose
    /// selected days are the complement of the provider's weekend.
    /// </summary>
    /// <param name="provider">The provider whose weekend semantics are projected onto a <see cref="WeekPattern" />.</param>
    /// <returns>The working-week <see cref="WeekPattern" /> implied by <paramref name="provider" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// Use this helper when migrating callers from the legacy <see cref="IWeekendDefinitionProvider" /> contract to the
    /// canonical <see cref="WeekPattern" /> model. The returned pattern can then be passed to APIs that accept a
    /// <see cref="WeekPattern" /> directly — for example, the <see cref="WeekPattern" />-accepting
    /// <c>NotableDateService</c> constructor.
    /// </para>
    /// <example>
    /// <code language="csharp">
    /// <![CDATA[
    /// IWeekendDefinitionProvider provider = new MyCustomWeekend();
    /// WeekPattern workingWeek = provider.ToWeekPattern();
    /// var service = new NotableDateService(ruleProviders, workingWeek);
    /// ]]>
    /// </code>
    /// </example>
    /// </remarks>
    public static WeekPattern ToWeekPattern(this IWeekendDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        WeekPattern pattern = WeekPattern.Empty;
        for (var i = 0; i < 7; i++)
        {
            DayOfWeek day = (DayOfWeek)i;
            if (!provider.IsWeekend(day))
                pattern = pattern.With(day);
        }

        return pattern;
    }
}
