// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TimeOfDayBitmapTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Contains unit tests for the internal <see cref="TimeOfDayBitmap" /> type.
/// </summary>
[TestClass]
public sealed partial class TimeOfDayBitmapTests
{
    /// <summary>The number of periods in a day at each sub-daily frequency: hours, minutes and seconds.</summary>
    private static readonly int[] s_dayLengths = [24, 1440, 86400];

    /// <summary>
    /// Builds a bitmap that allows the given times of day.
    /// </summary>
    /// <param name="length">The number of periods in a day.</param>
    /// <param name="allowed">The allowed times of day.</param>
    /// <returns>The bitmap.</returns>
    private static TimeOfDayBitmap Build(int length, IEnumerable<int> allowed)
    {
        var bitmap = new TimeOfDayBitmap(length);
        foreach (int timeOfDay in allowed)
        {
            bitmap.Add(timeOfDay);
        }

        return bitmap;
    }

    /// <summary>
    /// Returns the distance to the nearest allowed time of day at least a given distance away by visiting every
    /// candidate in turn, the reading <see cref="TimeOfDayBitmap.NextAllowedDistance" /> is held to.
    /// </summary>
    /// <param name="allowed">One flag per time of day, set when the time is allowed.</param>
    /// <param name="timeOfDay">The time of day the distance is measured from.</param>
    /// <param name="minimum">The smallest distance considered.</param>
    /// <param name="forward">Whether to measure forward through the day.</param>
    /// <returns>The distance, or zero when no allowed time lies at least <paramref name="minimum" /> away.</returns>
    private static int LinearDistance(bool[] allowed, int timeOfDay, int minimum, bool forward)
    {
        int length = allowed.Length;
        for (int distance = minimum; distance < length; distance++)
        {
            int candidate = forward
                ? (timeOfDay + distance) % length
                : (timeOfDay - distance + length) % length;

            if (allowed[candidate])
            {
                return distance;
            }
        }

        return 0;
    }

    /// <summary>
    /// Returns one flag per time of day, set for the allowed times.
    /// </summary>
    /// <param name="length">The number of periods in a day.</param>
    /// <param name="allowed">The allowed times of day.</param>
    /// <returns>The flags.</returns>
    private static bool[] Flags(int length, IEnumerable<int> allowed)
    {
        var flags = new bool[length];
        foreach (int timeOfDay in allowed)
        {
            flags[timeOfDay] = true;
        }

        return flags;
    }
}
