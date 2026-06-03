// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSystem.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Identifies the calendar system that a rule's strategy is expressed in.
/// </summary>
/// <remarks>
/// <para>
/// The first cut of the v2 engine supports the proleptic Gregorian calendar only. Additional calendar systems (for
/// example Hebrew or Islamic) are reserved for a later phase.
/// </para>
/// </remarks>
public enum CalendarSystem
{
    /// <summary>
    /// The proleptic Gregorian calendar.
    /// </summary>
    Gregorian = 0,
}
