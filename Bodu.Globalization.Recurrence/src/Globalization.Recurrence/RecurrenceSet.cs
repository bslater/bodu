// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Represents a composed set of recurring instants: one or more <see cref="RecurrenceRule" /> streams anchored at a
/// common start, merged with explicit recurrence dates (<c>RDATE</c>) and with exception dates (<c>EXDATE</c>) removed.
/// </summary>
/// <remarks>
/// <para>
/// The occurrence set is the ascending, duplicate-free union of every rule expansion and every explicit date, less any
/// instant that matches an exception date. Each rule is anchored at <see cref="Start" />, so the start instant is
/// emitted when a rule (or an explicit date) produces it — it is not added implicitly.
/// </para>
/// <para>
/// The set can be built programmatically through the constructor, or parsed from an iCalendar property block with
/// <see cref="Parse(string)" />.
/// </para>
/// </remarks>
/// <seealso cref="RecurrenceRule" />
public sealed partial class RecurrenceSet : IEquatable<RecurrenceSet>
{
    /// <summary>The rules whose expansions contribute occurrences.</summary>
    private readonly RecurrenceRule[] _rules;

    /// <summary>The explicit recurrence dates, sorted ascending.</summary>
    private readonly DateTime[] _dates;

    /// <summary>The exception dates removed from the occurrence set.</summary>
    private readonly DateTime[] _exceptionDates;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurrenceSet" /> class.
    /// </summary>
    /// <param name="start">The common start instant the rules are anchored to.</param>
    /// <param name="rules">The recurrence rules; may be empty when explicit dates are supplied.</param>
    /// <param name="dates">The explicit recurrence dates to add, or <see langword="null" /> for none.</param>
    /// <param name="exceptionDates">The exception dates to remove, or <see langword="null" /> for none.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rules" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when neither a rule nor an explicit date is supplied.</exception>
    public RecurrenceSet(
        DateTime start,
        IEnumerable<RecurrenceRule> rules,
        IEnumerable<DateTime>? dates = null,
        IEnumerable<DateTime>? exceptionDates = null)
    {
        ArgumentNullException.ThrowIfNull(rules);

        Start = start;
        _rules = rules.ToArray();
        _dates = dates?.ToArray() ?? [];
        _exceptionDates = exceptionDates?.ToArray() ?? [];

        if (_rules.Length == 0 && _dates.Length == 0)
        {
            throw new ArgumentException(RecurrenceResourceStrings.Arg_Invalid_RecurrenceSetEmpty, nameof(rules));
        }

        Array.Sort(_dates);
        Array.Sort(_exceptionDates);
    }

    /// <summary>
    /// Gets the common start instant the rules are anchored to.
    /// </summary>
    /// <value>The start (<c>DTSTART</c>) instant.</value>
    public DateTime Start { get; }

    /// <summary>
    /// Gets the recurrence rules that contribute to the occurrence set.
    /// </summary>
    /// <value>The rules, in the order supplied.</value>
    public IReadOnlyList<RecurrenceRule> Rules => _rules;

    /// <summary>
    /// Gets the explicit recurrence dates added to the occurrence set.
    /// </summary>
    /// <value>The explicit dates in ascending order.</value>
    public IReadOnlyList<DateTime> Dates => _dates;

    /// <summary>
    /// Gets the exception dates removed from the occurrence set.
    /// </summary>
    /// <value>The exception dates in ascending order.</value>
    public IReadOnlyList<DateTime> ExceptionDates => _exceptionDates;

    /// <summary>
    /// Determines whether this set is equal to another set by comparing the start, rules, dates, and exception dates.
    /// </summary>
    /// <param name="other">The set to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="other" /> is non-null with an equal start, equal rules in the same
    /// order, and equal date and exception-date lists; otherwise <see langword="false" />.
    /// </returns>
    public bool Equals(RecurrenceSet? other)
    {
        if (other is null)
        {
            return false;
        }

        if (Start != other.Start
            || _rules.Length != other._rules.Length
            || !_dates.AsSpan().SequenceEqual(other._dates)
            || !_exceptionDates.AsSpan().SequenceEqual(other._exceptionDates))
        {
            return false;
        }

        for (int i = 0; i < _rules.Length; i++)
        {
            if (!_rules[i].Equals(other._rules[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether this set is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="obj" /> is a <see cref="RecurrenceSet" /> equal to this instance;
    /// otherwise <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        Equals(obj as RecurrenceSet);

    /// <summary>
    /// Returns a hash code derived from the set's start and component counts.
    /// </summary>
    /// <returns>A hash code consistent with <see cref="Equals(RecurrenceSet)" />.</returns>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Start);
        hash.Add(_rules.Length);
        hash.Add(_dates.Length);
        hash.Add(_exceptionDates.Length);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Enumerates the occurrences of the set in ascending chronological order.
    /// </summary>
    /// <returns>
    /// The ascending, duplicate-free occurrences with exception dates removed. The sequence is unbounded when any
    /// contributing rule is unbounded; use <see cref="GetOccurrences(DateTime, DateTime)" /> or
    /// <see cref="Enumerable.Take{TSource}(IEnumerable{TSource}, int)" /> to bound it.
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when a contributing rule uses a sub-daily frequency.</exception>
    public IEnumerable<DateTime> GetOccurrences()
    {
        var exceptions = new HashSet<DateTime>(_exceptionDates);
        var queue = new PriorityQueue<IEnumerator<DateTime>, DateTime>();

        try
        {
            foreach (RecurrenceRule rule in _rules)
            {
                Seed(queue, rule.GetOccurrences(Start).GetEnumerator());
            }

            Seed(queue, ((IEnumerable<DateTime>)_dates).GetEnumerator());

            DateTime? last = null;
            while (queue.Count > 0)
            {
                IEnumerator<DateTime> source = queue.Dequeue();
                DateTime value = source.Current;

                if (source.MoveNext())
                {
                    queue.Enqueue(source, source.Current);
                }
                else
                {
                    source.Dispose();
                }

                if (last == value)
                {
                    continue;
                }

                last = value;
                if (!exceptions.Contains(value))
                {
                    yield return value;
                }
            }
        }
        finally
        {
            while (queue.Count > 0)
            {
                queue.Dequeue().Dispose();
            }
        }
    }

    /// <summary>
    /// Enumerates the occurrences of the set that fall within an inclusive window.
    /// </summary>
    /// <param name="from">The inclusive lower bound of the window.</param>
    /// <param name="to">The inclusive upper bound of the window.</param>
    /// <returns>The occurrences within <c>[from, to]</c> in ascending chronological order.</returns>
    /// <exception cref="NotSupportedException">Thrown when a contributing rule uses a sub-daily frequency.</exception>
    public IEnumerable<DateTime> GetOccurrences(DateTime from, DateTime to)
    {
        foreach (DateTime occurrence in GetOccurrences())
        {
            if (occurrence > to)
            {
                yield break;
            }

            if (occurrence >= from)
            {
                yield return occurrence;
            }
        }
    }

    /// <summary>
    /// Returns the first occurrence of the set that falls after the specified instant.
    /// </summary>
    /// <param name="after">The instant the returned occurrence must follow.</param>
    /// <param name="inclusive">
    /// <see langword="true" /> to allow an occurrence exactly equal to <paramref name="after" />; otherwise the
    /// occurrence must be strictly later.
    /// </param>
    /// <returns>The next occurrence, or <see langword="null" /> when the set produces none.</returns>
    /// <exception cref="NotSupportedException">Thrown when a contributing rule uses a sub-daily frequency.</exception>
    public DateTime? GetNextOccurrence(DateTime after, bool inclusive = false)
    {
        foreach (DateTime occurrence in GetOccurrences())
        {
            if (occurrence > after || (inclusive && occurrence == after))
            {
                return occurrence;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the last occurrence of the set that falls before the specified instant.
    /// </summary>
    /// <param name="before">The instant the returned occurrence must precede.</param>
    /// <param name="inclusive">
    /// <see langword="true" /> to allow an occurrence exactly equal to <paramref name="before" />; otherwise the
    /// occurrence must be strictly earlier.
    /// </param>
    /// <returns>
    /// The previous occurrence, or <see langword="null" /> when none precedes <paramref name="before" />.
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when a contributing rule uses a sub-daily frequency.</exception>
    /// <remarks>
    /// Due-ness evaluation is a previous-occurrence comparison — typically
    /// <c>lastCompleted &lt; GetPreviousOccurrence(now, inclusive: true)</c> — so missed occurrences coalesce
    /// structurally: the answer is a single instant, never a backlog.
    /// </remarks>
    public DateTime? GetPreviousOccurrence(DateTime before, bool inclusive = false)
    {
        DateTime? previous = null;
        foreach (DateTime occurrence in GetOccurrences())
        {
            if (occurrence > before || (!inclusive && occurrence == before))
            {
                break;
            }

            previous = occurrence;
        }

        return previous;
    }

    /// <summary>
    /// Returns the first occurrence of the set that falls after the specified instant, preserving its offset.
    /// </summary>
    /// <param name="after">The instant the returned occurrence must follow.</param>
    /// <param name="inclusive">
    /// <see langword="true" /> to allow an occurrence exactly equal to <paramref name="after" />; otherwise the
    /// occurrence must be strictly later.
    /// </param>
    /// <returns>
    /// The next occurrence carrying the offset of <paramref name="after" />, or <see langword="null" /> when the set
    /// produces none.
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when a contributing rule uses a sub-daily frequency.</exception>
    /// <remarks>
    /// The set's start and dates are wall-clock values; the query interprets them in the offset of
    /// <paramref name="after" /> and performs no other offset conversion.
    /// </remarks>
    public DateTimeOffset? GetNextOccurrence(DateTimeOffset after, bool inclusive = false)
    {
        DateTime? next = GetNextOccurrence(DateTime.SpecifyKind(after.DateTime, DateTimeKind.Unspecified), inclusive);
        return next is null ? null : new DateTimeOffset(next.Value, after.Offset);
    }

    /// <summary>
    /// Returns the last occurrence of the set that falls before the specified instant, preserving its offset.
    /// </summary>
    /// <param name="before">The instant the returned occurrence must precede.</param>
    /// <param name="inclusive">
    /// <see langword="true" /> to allow an occurrence exactly equal to <paramref name="before" />; otherwise the
    /// occurrence must be strictly earlier.
    /// </param>
    /// <returns>
    /// The previous occurrence carrying the offset of <paramref name="before" />, or <see langword="null" /> when none
    /// precedes it.
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when a contributing rule uses a sub-daily frequency.</exception>
    /// <remarks>
    /// The set's start and dates are wall-clock values; the query interprets them in the offset of
    /// <paramref name="before" /> and performs no other offset conversion.
    /// </remarks>
    public DateTimeOffset? GetPreviousOccurrence(DateTimeOffset before, bool inclusive = false)
    {
        DateTime? previous = GetPreviousOccurrence(DateTime.SpecifyKind(before.DateTime, DateTimeKind.Unspecified), inclusive);
        return previous is null ? null : new DateTimeOffset(previous.Value, before.Offset);
    }

    /// <summary>
    /// Advances the enumerator and, when it has a first element, enqueues it keyed on that element.
    /// </summary>
    /// <param name="queue">The merge priority queue.</param>
    /// <param name="source">The source enumerator to seed.</param>
    private static void Seed(PriorityQueue<IEnumerator<DateTime>, DateTime> queue, IEnumerator<DateTime> source)
    {
        if (source.MoveNext())
        {
            queue.Enqueue(source, source.Current);
        }
        else
        {
            source.Dispose();
        }
    }
}
