// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.NextOrSameDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.NextOrSameDateOfWeek(DateOnly, DayOfWeek)" /> returns the next
    /// on-or-after occurrence of the requested <see cref="DayOfWeek" /> for each <c>(input, target)</c> pair.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.NextOrSameDateOfWeekTestData), typeof(DateTimeExtensionsTests))]
    public void NextOrSameDateOfWeek_WhenCalled_ShouldReturnExpectedDate(DateTime inputDateTime, DayOfWeek targetDay, DateTime expectedDateTime)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        var expected = DateOnly.FromDateTime(expectedDateTime);

        DateOnly actual = input.NextOrSameDateOfWeek(targetDay);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NextOrSameDateOfWeek_WhenInvalidEnum_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 18);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.NextOrSameDateOfWeek((DayOfWeek)999);
        });
    }

    /// <summary>
    /// Verifies that targeting the input's own <see cref="DateOnly.DayOfWeek" /> returns the input itself,
    /// distinguishing the on-or-same variant from the strict
    /// <see cref="DateOnlyExtensions.NextDateOfWeek(DateOnly, DayOfWeek)" />.
    /// </summary>
    [TestMethod]
    public void NextOrSameDateOfWeek_WhenAlreadyOnTargetDay_ShouldReturnInputUnchanged()
    {
        var input = new DateOnly(2024, 4, 18); // Thursday
        DateOnly actual = input.NextOrSameDateOfWeek(DayOfWeek.Thursday);

        Assert.AreEqual(input, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnly.MaxValue" /> targeting its own <see cref="DateOnly.DayOfWeek" /> returns the
    /// input itself (no overflow).
    /// </summary>
    [TestMethod]
    public void NextOrSameDateOfWeek_WhenUsingMaxValue_ShouldReturnWithinRange()
    {
        DateOnly input = DateOnly.MaxValue;
        DateOnly actual = input.NextOrSameDateOfWeek(input.DayOfWeek);

        Assert.AreEqual(input, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnly.MinValue" /> returns a date on or after <see cref="DateOnly.MinValue" /> (does
    /// not underflow).
    /// </summary>
    [TestMethod]
    public void NextOrSameDateOfWeek_WhenUsingMinValue_ShouldReturnNextValidDate()
    {
        DateOnly actual = DateOnly.MinValue.NextOrSameDateOfWeek(DayOfWeek.Friday);

        Assert.IsGreaterThanOrEqualTo(DateOnly.MinValue, actual);
    }
}
