// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDaysOfWeekExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides bidirectional conversion between the <see cref="WorkingDaysOfWeek" /> enum sugar and the
/// <see cref="DayOfWeekSet" /> value type.
/// </summary>
public static class WorkingDaysOfWeekExtensions
{
    /// <summary>
    /// Returns the canonical <see cref="DayOfWeekSet" /> for the supplied <see cref="WorkingDaysOfWeek" /> value.
    /// </summary>
    /// <param name="value">The named working week to convert.</param>
    /// <returns>The <see cref="DayOfWeekSet" /> whose selected days match <paramref name="value" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a defined member of the <see cref="WorkingDaysOfWeek" />
    /// enumeration.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is <see cref="WorkingDaysOfWeek.Custom" />, which has no canonical set.
    /// </exception>
    public static DayOfWeekSet ToDayOfWeekSet(this WorkingDaysOfWeek value)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(value);
        return value == WorkingDaysOfWeek.Custom
            ? throw new ArgumentException(ResourceStrings.Arg_Invalid_CustomHasNoCanonicalDayOfWeekSet, nameof(value))
            : value switch
            {
                WorkingDaysOfWeek.MondayToFriday => DayOfWeekSet.MondayToFriday,
                WorkingDaysOfWeek.MondayToSaturday => DayOfWeekSet.MondayToSaturday,
                WorkingDaysOfWeek.MondayToThursdayAndSaturday => DayOfWeekSet.MondayToThursdayAndSaturday,
                WorkingDaysOfWeek.SaturdayToThursday => DayOfWeekSet.SaturdayToThursday,
                WorkingDaysOfWeek.SaturdayToWednesday => DayOfWeekSet.SaturdayToWednesday,
                WorkingDaysOfWeek.SundayToFriday => DayOfWeekSet.SundayToFriday,
                WorkingDaysOfWeek.SundayToThursday => DayOfWeekSet.SundayToThursday,
                WorkingDaysOfWeek.AllDays => DayOfWeekSet.All,
                _ => throw new ArgumentOutOfRangeException(nameof(value)),
            };
    }

    /// <summary>
    /// Returns the canonical <see cref="DayOfWeekSet" /> for the supplied <see cref="WorkingDaysOfWeek" /> value,
    /// consulting <paramref name="provider" /> when <paramref name="value" /> is
    /// <see cref="WorkingDaysOfWeek.Custom" />.
    /// </summary>
    /// <param name="value">The named working week to convert.</param>
    /// <param name="provider">
    /// A custom weekend provider consulted only when <paramref name="value" /> is
    /// <see cref="WorkingDaysOfWeek.Custom" />.
    /// </param>
    /// <returns>
    /// The <see cref="DayOfWeekSet" /> whose selected days match the working-week implied by <paramref name="value" />
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
    public static DayOfWeekSet ToDayOfWeekSet(this WorkingDaysOfWeek value, IWeekendDefinitionProvider? provider)
    {
        if (value == WorkingDaysOfWeek.Custom)
        {
            ThrowHelper.ThrowIfNull(provider);
            return provider.ToDayOfWeekSet();
        }

        return value.ToDayOfWeekSet();
    }

    /// <summary>
    /// Returns the matching <see cref="WorkingDaysOfWeek" /> value for the supplied <see cref="DayOfWeekSet" />, or
    /// <see cref="WorkingDaysOfWeek.Custom" /> when no named preset matches.
    /// </summary>
    /// <param name="workingWeek">The working week to identify.</param>
    /// <returns>
    /// The <see cref="WorkingDaysOfWeek" /> whose canonical set equals <paramref name="workingWeek" />, or
    /// <see cref="WorkingDaysOfWeek.Custom" /> when <paramref name="workingWeek" /> does not match any named preset.
    /// </returns>
    public static WorkingDaysOfWeek ToWorkingDaysOfWeek(this DayOfWeekSet workingWeek) =>
        TryGetWorkingDaysOfWeek(workingWeek, out WorkingDaysOfWeek value) ? value : WorkingDaysOfWeek.Custom;

    /// <summary>
    /// Attempts to identify the supplied <see cref="DayOfWeekSet" /> as a named <see cref="WorkingDaysOfWeek" />
    /// preset.
    /// </summary>
    /// <param name="workingWeek">The working week to identify.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, contains the matching <see cref="WorkingDaysOfWeek" />;
    /// otherwise, <see cref="WorkingDaysOfWeek.Custom" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="workingWeek" /> matches a named preset; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool TryGetWorkingDaysOfWeek(this DayOfWeekSet workingWeek, out WorkingDaysOfWeek value)
    {
        if (workingWeek == DayOfWeekSet.MondayToFriday)
        {
            value = WorkingDaysOfWeek.MondayToFriday;
            return true;
        }

        if (workingWeek == DayOfWeekSet.MondayToSaturday)
        {
            value = WorkingDaysOfWeek.MondayToSaturday;
            return true;
        }

        if (workingWeek == DayOfWeekSet.MondayToThursdayAndSaturday)
        {
            value = WorkingDaysOfWeek.MondayToThursdayAndSaturday;
            return true;
        }

        if (workingWeek == DayOfWeekSet.SaturdayToThursday)
        {
            value = WorkingDaysOfWeek.SaturdayToThursday;
            return true;
        }

        if (workingWeek == DayOfWeekSet.SaturdayToWednesday)
        {
            value = WorkingDaysOfWeek.SaturdayToWednesday;
            return true;
        }

        if (workingWeek == DayOfWeekSet.SundayToFriday)
        {
            value = WorkingDaysOfWeek.SundayToFriday;
            return true;
        }

        if (workingWeek == DayOfWeekSet.SundayToThursday)
        {
            value = WorkingDaysOfWeek.SundayToThursday;
            return true;
        }

        if (workingWeek == DayOfWeekSet.All)
        {
            value = WorkingDaysOfWeek.AllDays;
            return true;
        }

        value = WorkingDaysOfWeek.Custom;
        return false;
    }
}
