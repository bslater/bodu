// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TimeOfDayBitmapTests.Contains.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public sealed partial class TimeOfDayBitmapTests
{
    /// <summary>
    /// Verifies that the bitmap that allows every time of day has no length and contains every time of day.
    /// </summary>
    [TestMethod]
    public void Contains_WhenEveryTimeOfDay_ShouldContainEveryTime()
    {
        Assert.AreEqual(0, TimeOfDayBitmap.Every.Length);
        foreach (int timeOfDay in new[] { 0, 1, 63, 64, 1439, 86399 })
        {
            Assert.IsTrue(TimeOfDayBitmap.Every.Contains(timeOfDay), $"{timeOfDay}");
        }
    }

    /// <summary>
    /// Verifies that a bitmap contains exactly the times of day added to it, either side of each word boundary.
    /// </summary>
    [TestMethod]
    public void Contains_WhenTimesAreAdded_ShouldContainOnlyThoseTimes()
    {
        int[] added = [0, 63, 64, 127, 1000, 86399];
        TimeOfDayBitmap bitmap = Build(86400, added);

        Assert.AreEqual(86400, bitmap.Length);
        for (int timeOfDay = 0; timeOfDay < 86400; timeOfDay++)
        {
            Assert.AreEqual(Array.IndexOf(added, timeOfDay) >= 0, bitmap.Contains(timeOfDay), $"{timeOfDay}");
        }
    }
}
