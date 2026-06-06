// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.NextOrSameDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NextOrSameDateOfWeek(DateTime, DayOfWeek)" /> returns the next
    /// on-or-after occurrence of the requested <see cref="DayOfWeek" /> for each <c>(input, target)</c> pair.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NextOrSameDateOfWeekTestData))]
    public void NextOrSameDateOfWeek_WhenCalled_ShouldReturnExpectedDate(DateTime input, DayOfWeek targetDay, DateTime expected)
    {
        var actual = input.NextOrSameDateOfWeek(targetDay);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NextOrSameDateOfWeek_WhenInvalidEnum_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 4, 18);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.NextOrSameDateOfWeek((DayOfWeek)999);
        });
    }

    /// <summary>
    /// Verifies that targeting the input's own <see cref="DateTime.DayOfWeek" /> returns the input itself, distinguishing
    /// the on-or-same variant from the strict <see cref="DateTimeExtensions.NextDateOfWeek(DateTime, DayOfWeek)" />.
    /// </summary>
    [TestMethod]
    public void NextOrSameDateOfWeek_WhenAlreadyOnTargetDay_ShouldReturnInputUnchanged()
    {
        var input = new DateTime(2024, 4, 18, 17, 45, 00); // Thursday
        DateTime actual = input.NextOrSameDateOfWeek(DayOfWeek.Thursday);

        Assert.AreEqual(input, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NextOrSameDateOfWeek(DateTime, DayOfWeek)" /> preserves the input's
    /// <see cref="DateTime.Kind" /> across all <see cref="DateTimeKind" /> values.
    /// </summary>
    [TestMethod]
    [DataRow(DateTimeKind.Unspecified)]
    [DataRow(DateTimeKind.Utc)]
    [DataRow(DateTimeKind.Local)]
    public void NextOrSameDateOfWeek_WhenKindIsSet_ShouldPreserveKind(DateTimeKind kind)
    {
        var input = new DateTime(2024, 4, 18, 10, 0, 0, kind);
        DateTime actual = input.NextOrSameDateOfWeek(DayOfWeek.Wednesday);

        Assert.AreEqual(kind, actual.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NextOrSameDateOfWeek(DateTime, DayOfWeek)" /> preserves a
    /// sub-second-precision <see cref="DateTime.TimeOfDay" /> on the resulting date.
    /// </summary>
    [TestMethod]
    public void NextOrSameDateOfWeek_WhenTimeIsSet_ShouldPreserveTime()
    {
        var time = new TimeSpan(0, 12, 32, 55, 34, 903);
        var input = new DateTime(2024, 4, 18).Add(time);

        var actual = input.NextOrSameDateOfWeek(DayOfWeek.Monday).TimeOfDay;

        Assert.AreEqual(time, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTime.MaxValue" /> targeting its own <see cref="DateTime.DayOfWeek" /> returns the
    /// input itself (no overflow).
    /// </summary>
    [TestMethod]
    public void NextOrSameDateOfWeek_WhenUsingMaxValue_ShouldReturnWithinRange()
    {
        DateTime input = DateTime.MaxValue;
        DateTime actual = input.NextOrSameDateOfWeek(input.DayOfWeek);

        Assert.AreEqual(input, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTime.MinValue" /> returns a date on or after <see cref="DateTime.MinValue" /> (does
    /// not underflow).
    /// </summary>
    [TestMethod]
    public void NextOrSameDateOfWeek_WhenUsingMinValue_ShouldReturnNextValidDate()
    {
        DateTime actual = DateTime.MinValue.NextOrSameDateOfWeek(DayOfWeek.Friday);

        Assert.IsTrue(actual >= DateTime.MinValue);
    }
}
