// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TimeOfDayBitmapTests.NextAllowedDistance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public sealed partial class TimeOfDayBitmapTests
{
    /// <summary>
    /// Verifies that the distance to the nearest allowed time of day, forward and back, from every time of day and at
    /// every minimum distance, matches the linear scan over hourly and minutely days with a few times allowed.
    /// </summary>
    [TestMethod]
    public void NextAllowedDistance_WhenEveryStartAndMinimumOfShortDays_ShouldMatchTheLinearScan()
    {
        var random = new Random(20261005);
        foreach (int length in new[] { 24, 1440 })
        {
            foreach (int count in new[] { 1, 2, 5 })
            {
                var allowed = new HashSet<int>();
                while (allowed.Count < count)
                {
                    allowed.Add(random.Next(length));
                }

                TimeOfDayBitmap bitmap = Build(length, allowed);
                bool[] flags = Flags(length, allowed);
                int step = length == 24 ? 1 : 37;
                for (int timeOfDay = 0; timeOfDay < length; timeOfDay += step)
                {
                    for (int minimum = 1; minimum <= length; minimum += step)
                    {
                        foreach (bool forward in new[] { true, false })
                        {
                            Assert.AreEqual(
                                LinearDistance(flags, timeOfDay, minimum, forward),
                                bitmap.NextAllowedDistance(timeOfDay, minimum, forward),
                                $"length {length}, allowed [{string.Join(",", allowed)}], from {timeOfDay}, minimum {minimum}, forward {forward}");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Verifies that the distance to the nearest allowed time of day matches the linear scan over seeded days of every
    /// sub-daily length, from sparse to dense, at word boundaries, the ends of the day and seeded starts.
    /// </summary>
    [TestMethod]
    public void NextAllowedDistance_WhenSeededDaysOfEveryLength_ShouldMatchTheLinearScan()
    {
        var random = new Random(77);
        foreach (int length in s_dayLengths)
        {
            foreach (double density in new[] { 0.00002, 0.001, 0.05, 0.6, 0.999 })
            {
                var allowed = new HashSet<int>();
                for (int timeOfDay = 0; timeOfDay < length; timeOfDay++)
                {
                    if (random.NextDouble() < density)
                    {
                        allowed.Add(timeOfDay);
                    }
                }

                TimeOfDayBitmap bitmap = Build(length, allowed);
                bool[] flags = Flags(length, allowed);
                var starts = new List<int> { 0, 1, 62, 63, 64, 65, length - 65, length - 64, length - 63, length - 2, length - 1 };
                for (int i = 0; i < 24; i++)
                {
                    starts.Add(random.Next(length));
                }

                foreach (int timeOfDay in starts.Where(t => t >= 0 && t < length))
                {
                    foreach (int minimum in new[] { 1, 2, 63, 64, 65, length / 2, length - 1, length, random.Next(1, length) })
                    {
                        if (minimum < 1)
                        {
                            continue;
                        }

                        foreach (bool forward in new[] { true, false })
                        {
                            Assert.AreEqual(
                                LinearDistance(flags, timeOfDay, minimum, forward),
                                bitmap.NextAllowedDistance(timeOfDay, minimum, forward),
                                $"length {length}, density {density}, from {timeOfDay}, minimum {minimum}, forward {forward}");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Verifies that a day that allows only the time measured from, or no time at all, has no allowed time at any
    /// distance in either direction.
    /// </summary>
    [TestMethod]
    public void NextAllowedDistance_WhenOnlyTheStartIsAllowed_ShouldReturnZero()
    {
        foreach (int length in s_dayLengths)
        {
            foreach (int timeOfDay in new[] { 0, 63, 64, length - 1 }.Where(t => t < length))
            {
                TimeOfDayBitmap onlyStart = Build(length, [timeOfDay]);
                TimeOfDayBitmap none = Build(length, []);

                Assert.AreEqual(0, onlyStart.NextAllowedDistance(timeOfDay, 1, forward: true));
                Assert.AreEqual(0, onlyStart.NextAllowedDistance(timeOfDay, 1, forward: false));
                Assert.AreEqual(0, none.NextAllowedDistance(timeOfDay, 1, forward: true));
                Assert.AreEqual(0, none.NextAllowedDistance(timeOfDay, 1, forward: false));
            }
        }
    }

    /// <summary>
    /// Verifies that one allowed second of the day is found across the whole day, forward past midnight and back past
    /// it, from the second after it and the second before it.
    /// </summary>
    [TestMethod]
    public void NextAllowedDistance_WhenOneSecondADayIsAllowed_ShouldFindItAcrossMidnight()
    {
        const int Length = 86400;
        const int Allowed = (9 * 3600) + (30 * 60);
        TimeOfDayBitmap bitmap = Build(Length, [Allowed]);

        Assert.AreEqual(Length - 1, bitmap.NextAllowedDistance(Allowed + 1, 1, forward: true));
        Assert.AreEqual(1, bitmap.NextAllowedDistance(Allowed - 1, 1, forward: true));
        Assert.AreEqual(1, bitmap.NextAllowedDistance(Allowed + 1, 1, forward: false));
        Assert.AreEqual(Length - 1, bitmap.NextAllowedDistance(Allowed - 1, 1, forward: false));
        Assert.AreEqual(Allowed + 1, bitmap.NextAllowedDistance(Length - 1, 1, forward: true));
        Assert.AreEqual(Length - Allowed, bitmap.NextAllowedDistance(0, 1, forward: false));
    }
}
