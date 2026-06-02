// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.PreviousOrSameDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.PreviousOrSameDateOfWeek(DateOnly, DayOfWeek)" /> returns the prior
    /// on-or-before occurrence of the requested <see cref="DayOfWeek" /> for each <c>(input, target)</c> pair.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.PreviousOrSameDateOfWeekTestData), typeof(DateTimeExtensionsTests))]
    public void PreviousOrSameDateOfWeek_WhenCalled_ShouldReturnExpectedDate(DateTime inputDateTime, DayOfWeek targetDay, DateTime expectedDateTime)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        var expected = DateOnly.FromDateTime(expectedDateTime);

        var actual = input.PreviousOrSameDateOfWeek(targetDay);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void PreviousOrSameDateOfWeek_WhenEnumIsInvalid_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 18);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.PreviousOrSameDateOfWeek((DayOfWeek)999);
        });
    }

    /// <summary>
    /// Verifies that targeting the input's own <see cref="DateOnly.DayOfWeek" /> returns the input itself,
    /// distinguishing the on-or-same variant from the strict
    /// <see cref="DateOnlyExtensions.PreviousDateOfWeek(DateOnly, DayOfWeek)" />.
    /// </summary>
    [TestMethod]
    public void PreviousOrSameDateOfWeek_WhenAlreadyOnTargetDay_ShouldReturnInputUnchanged()
    {
        var input = new DateOnly(2024, 4, 18); // Thursday
        var actual = input.PreviousOrSameDateOfWeek(DayOfWeek.Thursday);

        Assert.AreEqual(input, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnly.MaxValue" /> targeting Saturday returns a valid result on or before
    /// <see cref="DateOnly.MaxValue" />.
    /// </summary>
    [TestMethod]
    public void PreviousOrSameDateOfWeek_WhenUsingMaxValue_ShouldSucceed()
    {
        var input = DateOnly.MaxValue;
        var actual = input.PreviousOrSameDateOfWeek(DayOfWeek.Saturday);

        Assert.IsTrue(actual <= DateOnly.MaxValue);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnly.MinValue" /> targeting its own <see cref="DateOnly.DayOfWeek" /> returns the
    /// input itself (no underflow).
    /// </summary>
    [TestMethod]
    public void PreviousOrSameDateOfWeek_WhenUsingMinValue_ShouldReturnSameOrGreater()
    {
        var input = DateOnly.MinValue;
        var actual = input.PreviousOrSameDateOfWeek(input.DayOfWeek);

        Assert.AreEqual(input, actual);
    }
}
