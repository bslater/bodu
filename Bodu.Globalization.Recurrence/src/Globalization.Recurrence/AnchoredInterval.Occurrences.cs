// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredInterval.Occurrences.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public sealed partial class AnchoredInterval
{
    /// <summary>
    /// Returns the first occurrence of the interval that falls after the specified instant.
    /// </summary>
    /// <param name="anchor">The instant the occurrence series is anchored to.</param>
    /// <param name="after">The instant the returned occurrence must follow.</param>
    /// <param name="inclusive">
    /// <see langword="true" /> to allow an occurrence exactly equal to <paramref name="after" />; otherwise the
    /// occurrence must be strictly later.
    /// </param>
    /// <returns>
    /// The next occurrence preserving the <see cref="DateTime.Kind" /> of <paramref name="anchor" />, or
    /// <see langword="null" /> when no further occurrence is representable.
    /// </returns>
    /// <remarks>
    /// The occurrence series is <c>anchor + k·interval</c> for <c>k ≥ 1</c>; when <paramref name="after" /> precedes
    /// the first occurrence, the first occurrence is returned. The instants are compared by their tick values;
    /// <see cref="DateTime.Kind" /> is never interpreted, so the caller is responsible for supplying commensurable
    /// arguments.
    /// </remarks>
    public DateTime? GetNextOccurrence(DateTime anchor, DateTime after, bool inclusive = false)
    {
        long k = NextIndex(after.Ticks - anchor.Ticks, inclusive);

        return OccurrenceAt(anchor, k);
    }

    /// <summary>
    /// Returns the last occurrence of the interval that falls before the specified instant.
    /// </summary>
    /// <param name="anchor">The instant the occurrence series is anchored to.</param>
    /// <param name="before">The instant the returned occurrence must precede.</param>
    /// <param name="inclusive">
    /// <see langword="true" /> to allow an occurrence exactly equal to <paramref name="before" />; otherwise the
    /// occurrence must be strictly earlier.
    /// </param>
    /// <returns>
    /// The previous occurrence preserving the <see cref="DateTime.Kind" /> of <paramref name="anchor" />, or
    /// <see langword="null" /> when no occurrence precedes <paramref name="before" />.
    /// </returns>
    /// <remarks>
    /// Because the series starts at <c>anchor + interval</c>, the query answers <see langword="null" /> until the first
    /// occurrence has passed — the anchor itself is never returned. Due-ness therefore stays the caller's comparison
    /// <c>lastCompleted &lt; GetPreviousOccurrence(now, inclusive: true)</c>, with missed occurrences coalescing
    /// structurally.
    /// </remarks>
    public DateTime? GetPreviousOccurrence(DateTime anchor, DateTime before, bool inclusive = false)
    {
        long k = PreviousIndex(before.Ticks - anchor.Ticks, inclusive);
        if (k < 1)
        {
            return null;
        }

        return anchor.AddTicks(k * Interval.Ticks);
    }

    /// <summary>
    /// Returns the first occurrence of the interval that falls after the specified instant, normalising between the
    /// arguments' offsets.
    /// </summary>
    /// <param name="anchor">The instant the occurrence series is anchored to.</param>
    /// <param name="after">The instant the returned occurrence must follow.</param>
    /// <param name="inclusive">
    /// <see langword="true" /> to allow an occurrence exactly equal to <paramref name="after" />; otherwise the
    /// occurrence must be strictly later.
    /// </param>
    /// <returns>
    /// The next occurrence carrying the offset of <paramref name="after" />, or <see langword="null" /> when no further
    /// occurrence is representable.
    /// </returns>
    /// <remarks>
    /// The arguments are compared as absolute instants, so an anchor supplied in UTC composes correctly with an
    /// <paramref name="after" /> in any other offset. No offset conversion is performed beyond that normalisation; the
    /// machine time zone is never consulted.
    /// </remarks>
    public DateTimeOffset? GetNextOccurrence(DateTimeOffset anchor, DateTimeOffset after, bool inclusive = false)
    {
        long k = NextIndex(after.UtcTicks - anchor.UtcTicks, inclusive);

        return OccurrenceAt(anchor, k, after.Offset);
    }

    /// <summary>
    /// Returns the last occurrence of the interval that falls before the specified instant, normalising between the
    /// arguments' offsets.
    /// </summary>
    /// <param name="anchor">The instant the occurrence series is anchored to.</param>
    /// <param name="before">The instant the returned occurrence must precede.</param>
    /// <param name="inclusive">
    /// <see langword="true" /> to allow an occurrence exactly equal to <paramref name="before" />; otherwise the
    /// occurrence must be strictly earlier.
    /// </param>
    /// <returns>
    /// The previous occurrence carrying the offset of <paramref name="before" />, or <see langword="null" /> when no
    /// occurrence precedes <paramref name="before" />.
    /// </returns>
    /// <remarks>
    /// The arguments are compared as absolute instants, so an anchor supplied in UTC composes correctly with a
    /// <paramref name="before" /> in any other offset. The anchor itself is never returned; see
    /// <see cref="GetPreviousOccurrence(DateTime, DateTime, bool)" /> for the due-ness rationale.
    /// </remarks>
    public DateTimeOffset? GetPreviousOccurrence(DateTimeOffset anchor, DateTimeOffset before, bool inclusive = false)
    {
        long k = PreviousIndex(before.UtcTicks - anchor.UtcTicks, inclusive);
        if (k < 1)
        {
            return null;
        }

        return OccurrenceAt(anchor, k, before.Offset);
    }

    /// <summary>
    /// Enumerates the occurrences of the interval anchored at the specified instant.
    /// </summary>
    /// <param name="anchor">The instant the occurrence series is anchored to.</param>
    /// <returns>
    /// The occurrences <c>anchor + k·interval</c> for <c>k ≥ 1</c> in ascending order, each preserving the
    /// <see cref="DateTime.Kind" /> of <paramref name="anchor" />. The sequence ends at the last representable
    /// occurrence.
    /// </returns>
    public IEnumerable<DateTime> GetOccurrences(DateTime anchor)
    {
        long interval = Interval.Ticks;
        long last = (DateTime.MaxValue.Ticks - anchor.Ticks) / interval;

        for (long k = 1; k <= last; k++)
        {
            yield return anchor.AddTicks(k * interval);
        }
    }

    /// <summary>
    /// Enumerates the occurrences of the interval that fall within an inclusive window.
    /// </summary>
    /// <param name="anchor">The instant the occurrence series is anchored to.</param>
    /// <param name="from">The inclusive lower bound of the window.</param>
    /// <param name="to">The inclusive upper bound of the window.</param>
    /// <returns>The occurrences within <c>[from, to]</c> in ascending chronological order.</returns>
    public IEnumerable<DateTime> GetOccurrences(DateTime anchor, DateTime from, DateTime to)
    {
        long interval = Interval.Ticks;
        long last = (DateTime.MaxValue.Ticks - anchor.Ticks) / interval;

        for (long k = NextIndex(from.Ticks - anchor.Ticks, inclusive: true); k <= last; k++)
        {
            DateTime occurrence = anchor.AddTicks(k * interval);
            if (occurrence > to)
            {
                yield break;
            }

            yield return occurrence;
        }
    }

    /// <summary>
    /// Enumerates the occurrences of the interval anchored at the specified instant, preserving its offset.
    /// </summary>
    /// <param name="anchor">The instant the occurrence series is anchored to.</param>
    /// <returns>
    /// The occurrences in ascending order, each carrying the offset of <paramref name="anchor" />. The sequence ends at
    /// the last representable occurrence.
    /// </returns>
    public IEnumerable<DateTimeOffset> GetOccurrences(DateTimeOffset anchor)
    {
        foreach (DateTime occurrence in GetOccurrences(DateTime.SpecifyKind(anchor.DateTime, DateTimeKind.Unspecified)))
        {
            yield return new DateTimeOffset(occurrence, anchor.Offset);
        }
    }

    /// <summary>
    /// Enumerates the occurrences of the interval that fall within an inclusive window, preserving the anchor's offset.
    /// </summary>
    /// <param name="anchor">The instant the occurrence series is anchored to.</param>
    /// <param name="from">The inclusive lower bound of the window.</param>
    /// <param name="to">The inclusive upper bound of the window.</param>
    /// <returns>The occurrences within <c>[from, to]</c> in ascending chronological order.</returns>
    /// <remarks>
    /// The window bounds are compared as absolute instants, so they may be supplied in offsets that differ from the
    /// anchor's; each returned occurrence carries the anchor's offset.
    /// </remarks>
    public IEnumerable<DateTimeOffset> GetOccurrences(DateTimeOffset anchor, DateTimeOffset from, DateTimeOffset to)
    {
        foreach (DateTimeOffset occurrence in GetOccurrences(anchor))
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
    /// Computes the one-based index of the first occurrence at or beyond the specified tick distance from the anchor.
    /// </summary>
    /// <param name="distance">The signed tick distance from the anchor to the query instant.</param>
    /// <param name="inclusive">Whether an occurrence exactly at the query instant qualifies.</param>
    /// <returns>The occurrence index, clamped to a minimum of one.</returns>
    private long NextIndex(long distance, bool inclusive)
    {
        long interval = Interval.Ticks;
        long quotient = FloorDiv(distance, interval);
        long k = inclusive && quotient * interval == distance ? quotient : quotient + 1;

        return Math.Max(k, 1);
    }

    /// <summary>
    /// Computes the one-based index of the last occurrence at or before the specified tick distance from the anchor.
    /// </summary>
    /// <param name="distance">The signed tick distance from the anchor to the query instant.</param>
    /// <param name="inclusive">Whether an occurrence exactly at the query instant qualifies.</param>
    /// <returns>The occurrence index, which is less than one when no occurrence qualifies.</returns>
    private long PreviousIndex(long distance, bool inclusive)
    {
        long interval = Interval.Ticks;
        long quotient = FloorDiv(distance, interval);

        return !inclusive && quotient * interval == distance ? quotient - 1 : quotient;
    }

    /// <summary>
    /// Returns the <paramref name="k" />-th occurrence after the anchor, or <see langword="null" /> when it is not
    /// representable.
    /// </summary>
    /// <param name="anchor">The anchor instant.</param>
    /// <param name="k">The one-based occurrence index.</param>
    /// <returns>The occurrence, or <see langword="null" /> past the end of the representable calendar.</returns>
    private DateTime? OccurrenceAt(DateTime anchor, long k)
    {
        long interval = Interval.Ticks;
        if (k > (DateTime.MaxValue.Ticks - anchor.Ticks) / interval)
        {
            return null;
        }

        return anchor.AddTicks(k * interval);
    }

    /// <summary>
    /// Returns the <paramref name="k" />-th occurrence after the anchor expressed in the specified offset, or
    /// <see langword="null" /> when it is not representable.
    /// </summary>
    /// <param name="anchor">The anchor instant.</param>
    /// <param name="k">The one-based occurrence index.</param>
    /// <param name="offset">The offset the returned occurrence carries.</param>
    /// <returns>The occurrence, or <see langword="null" /> past the end of the representable calendar.</returns>
    private DateTimeOffset? OccurrenceAt(DateTimeOffset anchor, long k, TimeSpan offset)
    {
        long interval = Interval.Ticks;
        if (k > (DateTime.MaxValue.Ticks - anchor.UtcTicks) / interval)
        {
            return null;
        }

        long utcTicks = anchor.UtcTicks + (k * interval);
        long localTicks = utcTicks + offset.Ticks;
        if (localTicks < DateTime.MinValue.Ticks || localTicks > DateTime.MaxValue.Ticks)
        {
            return null;
        }

        return new DateTimeOffset(localTicks, offset);
    }

    /// <summary>
    /// Divides rounding toward negative infinity, so occurrence indices are exact on either side of the anchor.
    /// </summary>
    /// <param name="dividend">The signed dividend.</param>
    /// <param name="divisor">The positive divisor.</param>
    /// <returns>The floor of the true quotient.</returns>
    private static long FloorDiv(long dividend, long divisor)
    {
        long quotient = dividend / divisor;
        if (dividend % divisor != 0 && dividend < 0)
        {
            quotient--;
        }

        return quotient;
    }
}
