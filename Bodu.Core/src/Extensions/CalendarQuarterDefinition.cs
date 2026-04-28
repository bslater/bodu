// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarQuarterDefinition.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Specifies how quarters (Q1–Q4) are defined within a given calendar-based system.
/// </summary>
/// <remarks>
/// This enumeration supports standard, regional, and historical calendar quarter systems. Use <see cref="Custom"/> for externally defined
/// or non-standard quarter rules.
/// </remarks>
public enum CalendarQuarterDefinition
{
    /// <summary>
    /// Quarters are based on the calendar year beginning on 1 January.
    /// <para>Q1 = 1 Jan–31 Mar, Q2 = 1 Apr–30 Jun, Q3 = 1 Jul–30 Sep, Q4 = 1 Oct–31 Dec.</para>
    /// </summary>
    JanuaryToDecember = 101,

    /// <summary>
    /// Quarters are based on a year beginning on 1 July.
    /// <para>Q1 = 1 Jul–30 Sep, Q2 = 1 Oct–31 Dec, Q3 = 1 Jan–31 Mar, Q4 = 1 Apr–30 Jun.</para>
    /// </summary>
    JulyToJune = 701,

    /// <summary>
    /// Quarters are based on a year beginning on 1 April.
    /// <para>Q1 = 1 Apr–30 Jun, Q2 = 1 Jul–30 Sep, Q3 = 1 Oct–31 Dec, Q4 = 1 Jan–31 Mar.</para>
    /// </summary>
    AprilToMarch = 401,

    /// <summary>
    /// Quarters begin on 6 April, aligned to the UK personal income tax year.
    /// <para>Q1 = 6 Apr–5 Jul, Q2 = 6 Jul–5 Oct, Q3 = 6 Oct–5 Jan, Q4 = 6 Jan–5 Apr.</para>
    /// </summary>
    April6ToApril5 = 406,

    /// <summary>
    /// Quarters are based on a historical civil calendar year beginning on 25 March (Lady Day).
    /// <para>Q1 = 25 Mar–24 Jun, Q2 = 25 Jun–24 Sep, Q3 = 25 Sep–24 Dec, Q4 = 25 Dec–24 Mar.</para>
    /// </summary>
    March25ToMarch24 = 325,

    /// <summary>
    /// Quarters are based on a year beginning on 1 October.
    /// <para>Q1 = 1 Oct–31 Dec, Q2 = 1 Jan–31 Mar, Q3 = 1 Apr–30 Jun, Q4 = 1 Jul–30 Sep.</para>
    /// </summary>
    OctoberToSeptember = 1001,

    /// <summary>
    /// Quarters are based on a year beginning on 1 February.
    /// <para>Q1 = 1 Feb–30 Apr, Q2 = 1 May–31 Jul, Q3 = 1 Aug–31 Oct, Q4 = 1 Nov–31 Jan.</para>
    /// </summary>
    FebruaryToJanuary = 201,

    /// <summary>
    /// Quarters are defined by an external or non-standard rule set.
    /// <para>Used for quarters based on weeks (e.g., 13-week, 4–4–5, or retail calendars) or other dynamic models.</para>
    /// Requires custom logic to compute quarter boundaries.
    /// </summary>
    Custom = -1,
}
