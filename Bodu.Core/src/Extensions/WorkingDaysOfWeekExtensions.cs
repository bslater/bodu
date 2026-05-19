// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDaysOfWeekExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides bidirectional conversion between the <see cref="WorkingDaysOfWeek" /> enum sugar and the
/// <see cref="WeekPattern" /> value type.
/// </summary>
public static class WorkingDaysOfWeekExtensions
{
    /// <summary>
    /// Returns the canonical <see cref="WeekPattern" /> for the supplied <see cref="WorkingDaysOfWeek" /> value.
    /// </summary>
    /// <param name="value">The named working-week pattern to convert.</param>
    /// <returns>The <see cref="WeekPattern" /> whose selected days match <paramref name="value" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a defined member of the <see cref="WorkingDaysOfWeek" />
    /// enumeration.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is <see cref="WorkingDaysOfWeek.Custom" />, which has no canonical
    /// pattern.
    /// </exception>
    public static WeekPattern ToWeekPattern(this WorkingDaysOfWeek value)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(value);
        return value == WorkingDaysOfWeek.Custom
            ? throw new ArgumentException("Custom has no canonical WeekPattern; pass a WeekPattern directly.", nameof(value)) //TODO: define a BCL-style exception message in the resx file and remove inline static text
            : value switch
            {
                WorkingDaysOfWeek.MondayToFriday => WeekPattern.MondayToFriday,
                WorkingDaysOfWeek.MondayToSaturday => WeekPattern.MondayToSaturday,
                WorkingDaysOfWeek.MondayToThursdayAndSaturday => WeekPattern.MondayToThursdayAndSaturday,
                WorkingDaysOfWeek.SaturdayToThursday => WeekPattern.SaturdayToThursday,
                WorkingDaysOfWeek.SaturdayToWednesday => WeekPattern.SaturdayToWednesday,
                WorkingDaysOfWeek.SundayToFriday => WeekPattern.SundayToFriday,
                WorkingDaysOfWeek.SundayToThursday => WeekPattern.SundayToThursday,
                WorkingDaysOfWeek.AllDays => WeekPattern.AllDays,
                _ => throw new ArgumentOutOfRangeException(nameof(value)),
            };
    }

    /// <summary>
    /// Returns the canonical <see cref="WeekPattern" /> for the supplied <see cref="WorkingDaysOfWeek" /> value,
    /// consulting <paramref name="provider" /> when <paramref name="value" /> is
    /// <see cref="WorkingDaysOfWeek.Custom" />.
    /// </summary>
    /// <param name="value">The named working-week pattern to convert.</param>
    /// <param name="provider">
    /// A custom weekend provider consulted only when <paramref name="value" /> is
    /// <see cref="WorkingDaysOfWeek.Custom" />.
    /// </param>
    /// <returns>
    /// The <see cref="WeekPattern" /> whose selected days match the working-week implied by <paramref name="value" />
    /// (and <paramref name="provider" /> when applicable).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a defined member of the <see cref="WorkingDaysOfWeek" />
    /// enumeration.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see cref="WorkingDaysOfWeek.Custom" /> and
    /// <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static WeekPattern ToWeekPattern(this WorkingDaysOfWeek value, IWeekendDefinitionProvider? provider)
    {
        if (value == WorkingDaysOfWeek.Custom)
        {
            ThrowHelper.ThrowIfNull(provider);
            return provider.ToWeekPattern();
        }

        return value.ToWeekPattern();
    }

    /// <summary>
    /// Returns the matching <see cref="WorkingDaysOfWeek" /> value for the supplied <see cref="WeekPattern" />, or
    /// <see cref="WorkingDaysOfWeek.Custom" /> when no named preset matches.
    /// </summary>
    /// <param name="pattern">The pattern to identify.</param>
    /// <returns>
    /// The <see cref="WorkingDaysOfWeek" /> whose canonical pattern equals <paramref name="pattern" />, or
    /// <see cref="WorkingDaysOfWeek.Custom" /> when <paramref name="pattern" /> does not match any named preset.
    /// </returns>
    public static WorkingDaysOfWeek ToWorkingDaysOfWeek(this WeekPattern pattern) =>
        TryGetWorkingDaysOfWeek(pattern, out WorkingDaysOfWeek value) ? value : WorkingDaysOfWeek.Custom;

    /// <summary>
    /// Attempts to identify the supplied <see cref="WeekPattern" /> as a named <see cref="WorkingDaysOfWeek" /> preset.
    /// </summary>
    /// <param name="pattern">The pattern to identify.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, contains the matching <see cref="WorkingDaysOfWeek" />;
    /// otherwise, <see cref="WorkingDaysOfWeek.Custom" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="pattern" /> matches a named preset; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool TryGetWorkingDaysOfWeek(this WeekPattern pattern, out WorkingDaysOfWeek value)
    {
        if (pattern == WeekPattern.MondayToFriday) { value = WorkingDaysOfWeek.MondayToFriday; return true; }
        if (pattern == WeekPattern.MondayToSaturday) { value = WorkingDaysOfWeek.MondayToSaturday; return true; }
        if (pattern == WeekPattern.MondayToThursdayAndSaturday) { value = WorkingDaysOfWeek.MondayToThursdayAndSaturday; return true; }
        if (pattern == WeekPattern.SaturdayToThursday) { value = WorkingDaysOfWeek.SaturdayToThursday; return true; }
        if (pattern == WeekPattern.SaturdayToWednesday) { value = WorkingDaysOfWeek.SaturdayToWednesday; return true; }
        if (pattern == WeekPattern.SundayToFriday) { value = WorkingDaysOfWeek.SundayToFriday; return true; }
        if (pattern == WeekPattern.SundayToThursday) { value = WorkingDaysOfWeek.SundayToThursday; return true; }
        if (pattern == WeekPattern.AllDays) { value = WorkingDaysOfWeek.AllDays; return true; }

        value = WorkingDaysOfWeek.Custom;
        return false;
    }
}
