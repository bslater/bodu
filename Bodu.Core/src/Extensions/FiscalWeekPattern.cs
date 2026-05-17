// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FiscalWeekPattern.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Specifies the week distribution pattern applied across the three fiscal periods within each quarter of a week-based
/// retail fiscal calendar.
/// </summary>
/// <remarks>
/// <para>
/// In a retail fiscal calendar, each quarter comprises exactly 13 weeks, divided into three fiscal periods. The pattern
/// determines how those 13 weeks are distributed — typically as a combination of one 5-week period and two 4-week
/// periods.
/// </para>
/// <para>
/// The chosen pattern does not affect quarter start or end boundaries, which are always separated by 13-week intervals.
/// It is relevant only when resolving fiscal period (fiscal month) boundaries within a quarter.
/// </para>
/// <para>
/// In a 53-week fiscal year, the additional week is always appended to the final period of Q4, extending it by one week
/// regardless of the configured pattern.
/// </para>
/// </remarks>
public enum FiscalWeekPattern
{
    /// <summary>
    /// Each quarter is divided into periods of 5, 4, and 4 weeks respectively.
    /// <para>
    /// Example for Q1: period 1 = weeks 1–5, period 2 = weeks 6–9, period 3 = weeks 10–13.
    /// </para>
    /// </summary>
    Weeks544,

    /// <summary>
    /// Each quarter is divided into periods of 4, 5, and 4 weeks respectively.
    /// <para>
    /// Example for Q1: period 1 = weeks 1–4, period 2 = weeks 5–9, period 3 = weeks 10–13.
    /// </para>
    /// </summary>
    Weeks454,

    /// <summary>
    /// Each quarter is divided into periods of 4, 4, and 5 weeks respectively.
    /// <para>
    /// Example for Q1: period 1 = weeks 1–4, period 2 = weeks 5–8, period 3 = weeks 9–13. In a 53-week fiscal year,
    /// Q4's third period extends to 6 weeks.
    /// </para>
    /// </summary>
    Weeks445,
}
