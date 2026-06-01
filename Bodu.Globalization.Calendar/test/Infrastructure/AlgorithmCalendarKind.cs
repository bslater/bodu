// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlgorithmCalendarKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Identifies the calendar system that an <see cref="INotableDateAlgorithm" /> known-answer test row should pass to
/// <see cref="INotableDateAlgorithm.GetDate(int, System.Globalization.Calendar?)" />.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Default" /> is the canonical "no calendar supplied" case — the algorithm receives <see langword="null" />
/// and applies its own default (Gregorian for every shipped algorithm). Test rows flagged as <see cref="Gregorian" />
/// or <see cref="Julian" /> instead construct the corresponding <see cref="System.Globalization.Calendar" /> instance
/// and pass it through, exercising the algorithm's non-default code paths.
/// </para>
/// <para>
/// Orthodox Easter Sunday is encoded as <see cref="Julian" /> — there is no dedicated Orthodox calendar in the Base
/// Class Library and the algorithm's Orthodox branch is reached by supplying a
/// <see cref="System.Globalization.JulianCalendar" />. Tests that previously required a separate Orthodox enum value
/// can group those rows under <see cref="Julian" /> in a provider method whose name carries the Orthodox provenance
/// instead.
/// </para>
/// </remarks>
public enum AlgorithmCalendarKind
{
    /// <summary>
    /// Pass <see langword="null" /> as the calendar argument. The algorithm uses its built-in default (Gregorian for
    /// every shipped implementation).
    /// </summary>
    Default = 0,

    /// <summary>
    /// Pass an explicit <see cref="System.Globalization.GregorianCalendar" /> instance. Exercises the algorithm's
    /// "calendar supplied" branch on a calendar that matches the default behaviour.
    /// </summary>
    Gregorian,

    /// <summary>
    /// Pass an explicit <see cref="System.Globalization.JulianCalendar" /> instance. Exercises the algorithm's
    /// non-Gregorian projection branch and is the encoding used for Orthodox Easter Sunday rows.
    /// </summary>
    Julian,
}
