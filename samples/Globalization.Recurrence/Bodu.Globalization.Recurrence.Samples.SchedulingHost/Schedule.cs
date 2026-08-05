// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Schedule.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence.Samples.SchedulingHost;

/// <summary>
/// A host-side adapter that presents all four schedule forms through one interface.
/// </summary>
/// <remarks>
/// <para>
/// The package deliberately does not ship this abstraction. The four forms differ in what they need
/// to answer a query — a rule and an interval need a series origin, a cron expression and a set do
/// not — and collapsing that difference is a hosting decision rather than a library one. What the
/// package does guarantee is that every form answers <c>GetNextOccurrence</c> and
/// <c>GetPreviousOccurrence</c> with the same inclusive-flag semantics, which is exactly what makes
/// an adapter this small possible.
/// </para>
/// <para>
/// The origin is captured here, at configuration time, so the query surface reduces to a single
/// instant regardless of form.
/// </para>
/// </remarks>
public sealed class Schedule
{
    private readonly Func<DateTime, bool, DateTime?> _next;
    private readonly Func<DateTime, bool, DateTime?> _previous;

    /// <summary>
    /// Initializes a new instance of the <see cref="Schedule" /> class.
    /// </summary>
    /// <param name="kind">The schedule form, for display.</param>
    /// <param name="text">The source text the schedule was parsed from.</param>
    /// <param name="next">The forward point query, bound to any origin the form requires.</param>
    /// <param name="previous">The backward point query, bound to the same origin.</param>
    private Schedule(string kind, string text, Func<DateTime, bool, DateTime?> next, Func<DateTime, bool, DateTime?> previous)
    {
        Kind = kind;
        Text = text;
        _next = next;
        _previous = previous;
    }

    /// <summary>
    /// Gets the schedule form this instance wraps.
    /// </summary>
    /// <value>One of <c>rrule</c>, <c>cron</c>, <c>interval</c>, or <c>set</c>.</value>
    public string Kind { get; }

    /// <summary>
    /// Gets the source text the schedule was parsed from.
    /// </summary>
    /// <value>The configured schedule text.</value>
    public string Text { get; }

    /// <summary>
    /// Attempts to build a schedule from a form name and its text.
    /// </summary>
    /// <param name="kind">The form name: <c>rrule</c>, <c>cron</c>, <c>interval</c>, or <c>set</c>.</param>
    /// <param name="text">The schedule text.</param>
    /// <param name="origin">The series origin, used by the forms that require one.</param>
    /// <param name="schedule">The parsed schedule when the method returns <see langword="true" />.</param>
    /// <param name="defect">The reason parsing failed when the method returns <see langword="false" />.</param>
    /// <returns><see langword="true" /> when the text parsed; otherwise <see langword="false" />.</returns>
    public static bool TryCreate(
        string kind,
        string text,
        DateTime origin,
        out Schedule? schedule,
        out string? defect)
    {
        schedule = null;

        switch (kind)
        {
            // A recurrence rule is pure syntax: the series origin is supplied per query, so the
            // adapter closes over the configured origin here.
            case "rrule":
                if (!RecurrenceRule.TryParse(text, out RecurrenceRule? rule, out defect))
                    return false;

                schedule = new Schedule(
                    kind,
                    text,
                    (from, inclusive) => rule.GetNextOccurrence(origin, from, inclusive),
                    (from, inclusive) => rule.GetPreviousOccurrence(origin, from, inclusive));
                return true;

            // A cron expression has no series origin at all -- it is a predicate over instants --
            // so the origin is simply unused.
            case "cron":
                if (!CronExpression.TryParse(text, out CronExpression? cron, out defect))
                    return false;

                schedule = new Schedule(
                    kind,
                    text,
                    (from, inclusive) => cron.GetNextOccurrence(from, inclusive),
                    (from, inclusive) => cron.GetPreviousOccurrence(from, inclusive));
                return true;

            // An anchored interval needs an anchor, which is the same role the origin plays.
            case "interval":
                if (!AnchoredInterval.TryParse(text, out AnchoredInterval? interval, out defect))
                    return false;

                schedule = new Schedule(
                    kind,
                    text,
                    (from, inclusive) => interval.GetNextOccurrence(origin, from, inclusive),
                    (from, inclusive) => interval.GetPreviousOccurrence(origin, from, inclusive));
                return true;

            // A recurrence set carries its own DTSTART, so the configured origin is ignored.
            case "set":
                if (!RecurrenceSet.TryParse(text, out RecurrenceSet? set, out defect))
                    return false;

                schedule = new Schedule(
                    kind,
                    text,
                    (from, inclusive) => set.GetNextOccurrence(from, inclusive),
                    (from, inclusive) => set.GetPreviousOccurrence(from, inclusive));
                return true;

            default:
                defect = $"Unknown schedule kind '{kind}'.";
                return false;
        }
    }

    /// <summary>
    /// Returns the first occurrence after an instant.
    /// </summary>
    /// <param name="after">The instant to search from.</param>
    /// <param name="inclusive">Whether an exact match on <paramref name="after" /> counts.</param>
    /// <returns>The occurrence, or <see langword="null" /> when the schedule has none.</returns>
    public DateTime? Next(DateTime after, bool inclusive = false) =>
        _next(after, inclusive);

    /// <summary>
    /// Returns the last occurrence before an instant.
    /// </summary>
    /// <param name="before">The instant to search back from.</param>
    /// <param name="inclusive">Whether an exact match on <paramref name="before" /> counts.</param>
    /// <returns>The occurrence, or <see langword="null" /> when the schedule has none.</returns>
    public DateTime? Previous(DateTime before, bool inclusive = false) =>
        _previous(before, inclusive);
}
