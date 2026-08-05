// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredInterval.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Represents a recurrence whose occurrences repeat at a fixed <see cref="Interval" /> from a caller-supplied anchor
/// instant, rather than being aligned to the calendar.
/// </summary>
/// <remarks>
/// <para>
/// The occurrence series of an anchored interval is <c>anchor + k·interval</c> for <c>k ≥ 1</c>: the anchor itself is
/// <b>not</b> an occurrence. The anchor models an instant such as "the last completed run", "first enrolment", or
/// "contract start" — its meaning is entirely the caller's; this type never interprets it. Like
/// <see cref="RecurrenceRule" />, the interval carries no anchor of its own: the anchor is supplied to every occurrence
/// query, so the same interval can be applied to different anchors.
/// </para>
/// <para>
/// Every answer is a pure function of the arguments. No API of this type reads the wall clock or consults the machine
/// time zone; due-ness therefore stays the caller's one-line comparison, typically
/// <c>lastCompleted &lt; GetPreviousOccurrence(now, inclusive: true)</c> over instants the caller supplies. Missed
/// occurrences coalesce structurally: an evaluation long after several missed occurrences answers identically to an
/// evaluation just after the first one, because the answer is an instant, not a backlog.
/// </para>
/// <para>
/// The canonical textual form is the RFC 5545 §3.3.6 duration grammar, for example <c>PT4H</c>, <c>P1D</c>,
/// <c>P1DT2H30M</c>, or <c>P2W</c>. Because that grammar carries no sub-second precision, the interval must be a
/// positive whole number of seconds, which keeps <see cref="ToString()" /> an exact round-trip through
/// <see cref="Parse(string)" />.
/// </para>
/// </remarks>
/// <seealso cref="RecurrenceRule" /> <seealso cref="CronExpression" />
public sealed partial class AnchoredInterval : IEquatable<AnchoredInterval>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnchoredInterval" /> class.
    /// </summary>
    /// <param name="interval">The spacing between successive occurrences.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="interval" /> is zero or negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="interval" /> carries sub-second ticks, which the canonical iCalendar duration text
    /// cannot represent.
    /// </exception>
    public AnchoredInterval(TimeSpan interval)
    {
        RecurrenceThrowHelper.ThrowIfInvalidAnchoredInterval(interval);

        Interval = interval;
    }

    /// <summary>
    /// Gets the spacing between successive occurrences.
    /// </summary>
    /// <value>The positive, whole-second interval between occurrences.</value>
    public TimeSpan Interval { get; }

    /// <summary>
    /// Determines whether this interval is equal to another interval.
    /// </summary>
    /// <param name="other">The interval to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="other" /> is non-null and represents the same
    /// <see cref="Interval" />; otherwise <see langword="false" />.
    /// </returns>
    public bool Equals(AnchoredInterval? other) =>
        other is not null && Interval == other.Interval;

    /// <summary>
    /// Determines whether this interval is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="obj" /> is an <see cref="AnchoredInterval" /> equal to this
    /// instance; otherwise <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        Equals(obj as AnchoredInterval);

    /// <summary>
    /// Returns a hash code for the interval.
    /// </summary>
    /// <returns>A hash code consistent with <see cref="Equals(AnchoredInterval)" />.</returns>
    public override int GetHashCode() =>
        Interval.GetHashCode();
}
