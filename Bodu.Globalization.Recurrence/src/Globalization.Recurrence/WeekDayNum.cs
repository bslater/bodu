// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekDayNum.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Represents a single entry in an RFC 5545 <c>BYDAY</c> rule part: a day of the week with an optional ordinal that
/// selects a specific occurrence of that weekday within the frequency period.
/// </summary>
/// <param name="Ordinal">
/// The ordinal occurrence of <paramref name="Day" /> within the period. Zero selects every occurrence; a positive value
/// counts forward from the start of the period (for example <c>1</c> is the first) and a negative value counts backward
/// from the end (for example <c>-1</c> is the last).
/// </param>
/// <param name="Day">The day of the week the entry selects.</param>
/// <remarks>
/// <para>
/// An ordinal is only meaningful when the enclosing rule is monthly or yearly; for a weekly rule the ordinal is always
/// zero. The textual form places the ordinal before the two-letter weekday token, so <c>-1FR</c> parses to an entry
/// with an ordinal of <c>-1</c> and a day of <see cref="DayOfWeek.Friday" />.
/// </para>
/// </remarks>
public readonly record struct WeekDayNum(int Ordinal, DayOfWeek Day)
{
    /// <summary>
    /// Gets a value indicating whether the entry selects every occurrence of <see cref="Day" /> rather than a single
    /// positional occurrence.
    /// </summary>
    /// <value><see langword="true" /> when <see cref="Ordinal" /> is zero; otherwise <see langword="false" />.</value>
    public bool IsEveryOccurrence =>
        Ordinal == 0;
}
