// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayProximity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Specifies how a <see cref="DateResolutionStrategy.WeekdayNearDate" /> rule positions its target weekday relative to
/// the rule's fixed reference month and day.
/// </summary>
/// <remarks>
/// <para>
/// Because a given weekday recurs every seven days, each direction selects a single unambiguous occurrence within a
/// seven-day window anchored at the reference date:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="OnOrAfter" /> — the reference date itself when it already falls on the target weekday, otherwise the
/// first such weekday in the following six days.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="OnOrBefore" /> — the reference date itself when it already falls on the target weekday, otherwise the
/// most recent such weekday in the preceding six days.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Nearest" /> — the closest occurrence of the target weekday in either direction. Because the forward and
/// backward distances always sum to seven, they are never equal, so the nearest occurrence is unique.
/// </description>
/// </item>
/// </list>
/// </remarks>
public enum WeekdayProximity
{
    /// <summary>
    /// Selects the target weekday falling on or after the reference date (for example, "the Saturday on or after 20
    /// June" for Nordic Midsummer Day).
    /// </summary>
    OnOrAfter = 0,

    /// <summary>
    /// Selects the target weekday falling on or before the reference date (for example, "the Wednesday on or before 22
    /// November" for German Repentance Day, the Wednesday before 23 November).
    /// </summary>
    OnOrBefore,

    /// <summary>
    /// Selects the occurrence of the target weekday nearest to the reference date in either direction (for example,
    /// "the Monday nearest to a given date").
    /// </summary>
    Nearest,
}
