// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSet.Presets.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct DayOfWeekSet
{
    /// <summary>
    /// Gets the set of Monday to Friday.
    /// </summary>
    /// <remarks>
    /// Equal to <see cref="MondayToFriday" />, and corresponds to <see cref="WorkingDaysOfWeek.MondayToFriday" />.
    /// </remarks>
    public static DayOfWeekSet Weekdays { get; } = new(
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);

    /// <summary>
    /// Gets the set of Saturday and Sunday.
    /// </summary>
    public static DayOfWeekSet Weekend { get; } = new(DayOfWeek.Saturday, DayOfWeek.Sunday);

    /// <summary>
    /// Gets the working week of Monday to Friday.
    /// </summary>
    /// <remarks>
    /// Equal to <see cref="Weekdays" />, and corresponds to <see cref="WorkingDaysOfWeek.MondayToFriday" />.
    /// </remarks>
    public static DayOfWeekSet MondayToFriday { get; } = Weekdays;

    /// <summary>
    /// Gets the working week of Monday to Saturday.
    /// </summary>
    /// <remarks>
    /// Corresponds to <see cref="WorkingDaysOfWeek.MondayToSaturday" />.
    /// </remarks>
    public static DayOfWeekSet MondayToSaturday { get; } = new(
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday);

    /// <summary>
    /// Gets the working week of Monday to Thursday and Saturday.
    /// </summary>
    /// <remarks>
    /// Corresponds to <see cref="WorkingDaysOfWeek.MondayToThursdayAndSaturday" />.
    /// </remarks>
    public static DayOfWeekSet MondayToThursdayAndSaturday { get; } = new(
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday);

    /// <summary>
    /// Gets the working week of Saturday to Thursday.
    /// </summary>
    /// <remarks>
    /// Corresponds to <see cref="WorkingDaysOfWeek.SaturdayToThursday" />.
    /// </remarks>
    public static DayOfWeekSet SaturdayToThursday { get; } = new(
        DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday);

    /// <summary>
    /// Gets the working week of Saturday to Wednesday.
    /// </summary>
    /// <remarks>
    /// Corresponds to <see cref="WorkingDaysOfWeek.SaturdayToWednesday" />.
    /// </remarks>
    public static DayOfWeekSet SaturdayToWednesday { get; } = new(
        DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday);

    /// <summary>
    /// Gets the working week of Sunday to Friday.
    /// </summary>
    /// <remarks>
    /// Corresponds to <see cref="WorkingDaysOfWeek.SundayToFriday" />.
    /// </remarks>
    public static DayOfWeekSet SundayToFriday { get; } = new(
        DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);

    /// <summary>
    /// Gets the working week of Sunday to Thursday.
    /// </summary>
    /// <remarks>
    /// Corresponds to <see cref="WorkingDaysOfWeek.SundayToThursday" />.
    /// </remarks>
    public static DayOfWeekSet SundayToThursday { get; } = new(
        DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday);
}
