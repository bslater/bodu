// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TimeOfDayBitmap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Represents the times of day a sub-daily recurrence rule's periods may occupy, one bit per period of a day, and finds
/// the nearest allowed time in either direction.
/// </summary>
/// <remarks>
/// A summary holds one bit per word of the bitmap, set when the word allows any time, so a search passes over an empty
/// stretch of the day sixty-four words at a time, and within a word goes straight to the nearest allowed time by
/// counting zero bits. A secondly rule that allows one second a day has 1,350 words, and a search reads a few of them
/// and at most 22 summary words, rather than every word between the time it starts from and the allowed one.
/// </remarks>
internal sealed class TimeOfDayBitmap
{
    /// <summary>The bitmap that allows every time of day, which a rule that limits no time of day uses.</summary>
    internal static readonly TimeOfDayBitmap Every = new(0);

    /// <summary>One bit per time of day, set when the time is allowed.</summary>
    private readonly ulong[] _words;

    /// <summary>One bit per word of <see cref="_words" />, set when that word allows any time.</summary>
    private readonly ulong[] _summary;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeOfDayBitmap" /> class that allows no time of day yet.
    /// </summary>
    /// <param name="length">The number of periods in a day, or zero for a bitmap that allows every time of day.</param>
    internal TimeOfDayBitmap(int length)
    {
        Length = length;
        _words = new ulong[(length + 63) >> 6];
        _summary = new ulong[(_words.Length + 63) >> 6];
    }

    /// <summary>
    /// Gets the number of periods in a day the bitmap covers.
    /// </summary>
    /// <value>The number of periods in a day, or zero when the bitmap allows every time of day.</value>
    internal int Length { get; }

    /// <summary>
    /// Allows a time of day.
    /// </summary>
    /// <param name="timeOfDay">The time of day, in periods since midnight.</param>
    internal void Add(int timeOfDay)
    {
        int word = timeOfDay >> 6;
        _words[word] |= 1UL << (timeOfDay & 63);
        _summary[word >> 6] |= 1UL << (word & 63);
    }

    /// <summary>
    /// Determines whether a time of day is allowed.
    /// </summary>
    /// <param name="timeOfDay">The time of day, in periods since midnight.</param>
    /// <returns><see langword="true" /> when the time of day is allowed; otherwise <see langword="false" />.</returns>
    internal bool Contains(int timeOfDay) =>
        Length == 0 || (_words[timeOfDay >> 6] & (1UL << (timeOfDay & 63))) != 0;

    /// <summary>
    /// Returns the distance from a time of day to the nearest allowed time of day at least a given distance away.
    /// </summary>
    /// <param name="timeOfDay">The time of day the distance is measured from, in periods since midnight.</param>
    /// <param name="minimum">The smallest distance considered, at least one.</param>
    /// <param name="forward">
    /// <see langword="true" /> to measure forward through the day; <see langword="false" /> to measure back. Either way
    /// the distance wraps round midnight.
    /// </param>
    /// <returns>
    /// The distance, below <see cref="Length" />, or zero when no allowed time lies at least
    /// <paramref name="minimum" /> away.
    /// </returns>
    internal int NextAllowedDistance(int timeOfDay, int minimum, bool forward)
    {
        int length = Length;
        if (minimum >= length)
        {
            return 0;
        }

        // The candidates run from the time minimum away round to the time beside timeOfDay, so they form one stretch
        // of the bitmap or, where they cross midnight, two.
        int found;
        if (forward)
        {
            int first = (timeOfDay + minimum) % length;
            if (first < timeOfDay)
            {
                found = FirstAllowed(first, timeOfDay);
            }
            else
            {
                found = FirstAllowed(first, length);
                if (found < 0)
                {
                    found = FirstAllowed(0, timeOfDay);
                }
            }

            return found < 0 ? 0 : (found - timeOfDay + length) % length;
        }

        int last = (timeOfDay - minimum + length) % length;
        if (last > timeOfDay)
        {
            found = LastAllowed(last, timeOfDay + 1);
        }
        else
        {
            found = LastAllowed(last, 0);
            if (found < 0)
            {
                found = LastAllowed(length - 1, timeOfDay + 1);
            }
        }

        return found < 0 ? 0 : (timeOfDay - found + length) % length;
    }

    /// <summary>
    /// Returns the first allowed time of day in a stretch of the bitmap.
    /// </summary>
    /// <param name="from">The first time of day in the stretch.</param>
    /// <param name="end">The time of day the stretch ends before.</param>
    /// <returns>
    /// The first allowed time at or after <paramref name="from" /> and before <paramref name="end" />, or -1.
    /// </returns>
    private int FirstAllowed(int from, int end)
    {
        if (from >= end)
        {
            return -1;
        }

        int word = from >> 6;
        ulong bits = _words[word] & (ulong.MaxValue << (from & 63));
        if (bits == 0)
        {
            word = FirstNonEmptyWord(word + 1);
            if (word < 0 || (word << 6) >= end)
            {
                return -1;
            }

            bits = _words[word];
        }

        int found = (word << 6) + BitOperations.TrailingZeroCount(bits);
        return found < end ? found : -1;
    }

    /// <summary>
    /// Returns the last allowed time of day in a stretch of the bitmap.
    /// </summary>
    /// <param name="from">The last time of day in the stretch.</param>
    /// <param name="lowest">The first time of day in the stretch.</param>
    /// <returns>
    /// The last allowed time at or before <paramref name="from" /> and at or after <paramref name="lowest" />, or -1.
    /// </returns>
    private int LastAllowed(int from, int lowest)
    {
        if (from < lowest)
        {
            return -1;
        }

        int word = from >> 6;
        ulong bits = _words[word] & (ulong.MaxValue >> (63 - (from & 63)));
        if (bits == 0)
        {
            word = LastNonEmptyWord(word - 1);
            if (word < 0 || (word << 6) + 63 < lowest)
            {
                return -1;
            }

            bits = _words[word];
        }

        int found = (word << 6) + 63 - BitOperations.LeadingZeroCount(bits);
        return found >= lowest ? found : -1;
    }

    /// <summary>
    /// Returns the first word of the bitmap, at or after a given one, that allows any time.
    /// </summary>
    /// <param name="from">The index of the first word considered.</param>
    /// <returns>The index of the word, or -1 when none allows a time.</returns>
    private int FirstNonEmptyWord(int from)
    {
        if (from >= _words.Length)
        {
            return -1;
        }

        int index = from >> 6;
        ulong bits = _summary[index] & (ulong.MaxValue << (from & 63));
        while (bits == 0)
        {
            if (++index == _summary.Length)
            {
                return -1;
            }

            bits = _summary[index];
        }

        return (index << 6) + BitOperations.TrailingZeroCount(bits);
    }

    /// <summary>
    /// Returns the last word of the bitmap, at or before a given one, that allows any time.
    /// </summary>
    /// <param name="from">The index of the last word considered.</param>
    /// <returns>The index of the word, or -1 when none allows a time.</returns>
    private int LastNonEmptyWord(int from)
    {
        if (from < 0)
        {
            return -1;
        }

        int index = from >> 6;
        ulong bits = _summary[index] & (ulong.MaxValue >> (63 - (from & 63)));
        while (bits == 0)
        {
            if (--index < 0)
            {
                return -1;
            }

            bits = _summary[index];
        }

        return (index << 6) + 63 - BitOperations.LeadingZeroCount(bits);
    }
}
