// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayMathTests.SeekOrNull.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class WeekdayMathTests
{
    /// <summary>
    /// Verifies that for an anchor comfortably inside the representable range (the fast path), <c>SeekOrNull</c>
    /// returns the same date as <c>Seek</c> across every proximity rule.
    /// </summary>
    /// <param name="proximity">The proximity rule under test.</param>
    [TestMethod]
    [DataRow(WeekdayProximity.Before)]
    [DataRow(WeekdayProximity.OnOrBefore)]
    [DataRow(WeekdayProximity.Nearest)]
    [DataRow(WeekdayProximity.OnOrAfter)]
    [DataRow(WeekdayProximity.After)]
    public void SeekOrNull_WhenAnchorInRange_ShouldMatchSeek(WeekdayProximity proximity)
    {
        var anchor = new DateOnly(2026, 7, 3);

        foreach (DayOfWeek dow in Enum.GetValues<DayOfWeek>())
        {
            DateOnly expected = WeekdayMath.Seek(anchor, dow, proximity);
            Assert.AreEqual(expected, WeekdayMath.SeekOrNull(anchor, dow, proximity),
                $"SeekOrNull diverged from Seek for {dow}/{proximity}.");
        }
    }

    /// <summary>
    /// Verifies that seeking strictly after any weekday from <see cref="DateOnly.MaxValue" /> rolls past the end of
    /// the representable range and returns <see langword="null" /> instead of throwing.
    /// </summary>
    [TestMethod]
    public void SeekOrNull_WhenSeekRollsPastMaxValue_ShouldReturnNull()
    {
        foreach (DayOfWeek dow in Enum.GetValues<DayOfWeek>())
            Assert.IsNull(WeekdayMath.SeekOrNull(DateOnly.MaxValue, dow, WeekdayProximity.After),
                $"A strictly-after seek from MaxValue for {dow} must roll past the range and return null.");
    }

    /// <summary>
    /// Verifies that seeking strictly before any weekday from <see cref="DateOnly.MinValue" /> rolls past the start
    /// of the representable range and returns <see langword="null" /> instead of throwing.
    /// </summary>
    [TestMethod]
    public void SeekOrNull_WhenSeekRollsPastMinValue_ShouldReturnNull()
    {
        foreach (DayOfWeek dow in Enum.GetValues<DayOfWeek>())
            Assert.IsNull(WeekdayMath.SeekOrNull(DateOnly.MinValue, dow, WeekdayProximity.Before),
                $"A strictly-before seek from MinValue for {dow} must roll past the range and return null.");
    }

    /// <summary>
    /// Verifies that an anchor inside the last representable week whose seek just fits within the range (a six-day
    /// move landing exactly on <see cref="DateOnly.MaxValue" />) returns that date rather than <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void SeekOrNull_WhenSeekJustFitsWithinBoundaryWeek_ShouldReturnDate()
    {
        DateOnly anchor = DateOnly.MaxValue.AddDays(-6);

        // The first on-or-after occurrence of MaxValue's own weekday is MaxValue itself, six days ahead.
        Assert.AreEqual(DateOnly.MaxValue,
            WeekdayMath.SeekOrNull(anchor, DateOnly.MaxValue.DayOfWeek, WeekdayProximity.OnOrAfter));
    }
}
