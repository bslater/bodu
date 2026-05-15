// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPattern.Presets.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial struct WeekPattern
{
#pragma warning disable IDE1006

    /// <summary>
    /// Represents a working-week <see cref="WeekPattern" /> with Monday through Friday selected.
    /// </summary>
    /// <remarks>This is an alias for <see cref="Weekdays" /> and corresponds to <see cref="WorkingDaysOfWeek.MondayToFriday" />.</remarks>
    public static readonly WeekPattern MondayToFriday = new WeekPattern(
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);

    /// <summary>
    /// Represents a working-week <see cref="WeekPattern" /> with Monday through Saturday selected.
    /// </summary>
    /// <remarks>Corresponds to <see cref="WorkingDaysOfWeek.MondayToSaturday" />.</remarks>
    public static readonly WeekPattern MondayToSaturday = new WeekPattern(
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday);

    /// <summary>
    /// Represents a working-week <see cref="WeekPattern" /> with Monday through Thursday and Saturday selected.
    /// </summary>
    /// <remarks>Corresponds to <see cref="WorkingDaysOfWeek.MondayToThursdayAndSaturday" />.</remarks>
    public static readonly WeekPattern MondayToThursdayAndSaturday = new WeekPattern(
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday);

    /// <summary>
    /// Represents a working-week <see cref="WeekPattern" /> with Saturday through Thursday selected.
    /// </summary>
    /// <remarks>Corresponds to <see cref="WorkingDaysOfWeek.SaturdayToThursday" />.</remarks>
    public static readonly WeekPattern SaturdayToThursday = new WeekPattern(
        DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday);

    /// <summary>
    /// Represents a working-week <see cref="WeekPattern" /> with Saturday through Wednesday selected.
    /// </summary>
    /// <remarks>Corresponds to <see cref="WorkingDaysOfWeek.SaturdayToWednesday" />.</remarks>
    public static readonly WeekPattern SaturdayToWednesday = new WeekPattern(
        DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday);

    /// <summary>
    /// Represents a working-week <see cref="WeekPattern" /> with Sunday through Friday selected.
    /// </summary>
    /// <remarks>Corresponds to <see cref="WorkingDaysOfWeek.SundayToFriday" />.</remarks>
    public static readonly WeekPattern SundayToFriday = new WeekPattern(
        DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);

    /// <summary>
    /// Represents a working-week <see cref="WeekPattern" /> with Sunday through Thursday selected.
    /// </summary>
    /// <remarks>Corresponds to <see cref="WorkingDaysOfWeek.SundayToThursday" />.</remarks>
    public static readonly WeekPattern SundayToThursday = new WeekPattern(
        DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday);

    /// <summary>
    /// Represents a working-week <see cref="WeekPattern" /> with every day of the week selected.
    /// </summary>
    /// <remarks>Corresponds to <see cref="WorkingDaysOfWeek.AllDays" />.</remarks>
    public static readonly WeekPattern AllDays = new WeekPattern(
        DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday);

#pragma warning restore IDE1006
}
