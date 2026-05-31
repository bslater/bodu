// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.AddWorkingDays.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that a positive number of days advances forward.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenPositiveDays_ShouldAdvanceForward()
    {
        NotableDateService service = BuildService();

        DateOnly result = new DateOnly(2026, 1, 5).AddWorkingDays(service, days: 3);

        Assert.AreEqual(new DateOnly(2026, 1, 8), result);
    }

    /// <summary>
    /// Verifies that a negative number of days retreats backward.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenNegativeDays_ShouldRetreatBackward()
    {
        NotableDateService service = BuildService();

        DateOnly result = new DateOnly(2026, 1, 8).AddWorkingDays(service, days: -3);

        Assert.AreEqual(new DateOnly(2026, 1, 5), result);
    }

    /// <summary>
    /// Verifies that zero days returns the input unchanged regardless of whether it is a working day.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenZeroDays_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        var weekend = new DateOnly(2026, 1, 3);

        DateOnly result = weekend.AddWorkingDays(service, days: 0);

        Assert.AreEqual(weekend, result);
    }

    /// <summary>
    /// Verifies that zero days returns the input unchanged even when the input matches a non-working rule.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenZeroDaysOnNonWorkingRuleDay_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 1, nonWorking: true));
        var input = new DateOnly(2026, 1, 1);

        DateOnly result = input.AddWorkingDays(service, days: 0);

        Assert.AreEqual(input, result);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService();
        try
        {
            NotableDateContext.Default = service;

            DateOnly result = new DateOnly(2026, 1, 5).AddWorkingDays(2);

            Assert.AreEqual(new DateOnly(2026, 1, 7), result);
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenServiceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 6).AddWorkingDays(service: null!, days: 1);
        });
    }

    /// <summary>
    /// Verifies that signed-day arithmetic produces the expected working-day result across positive, negative and zero values.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AddWorkingDaysSignedTestData))]
    public void AddWorkingDays_WhenSignedDaysSupplied_ShouldReturnExpectedWorkingDay(DateOnly input, int days, DateOnly expected)
    {
        NotableDateService service = BuildService();

        DateOnly actual = input.AddWorkingDays(service, days);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that adding working days that would overrun <see cref="DateOnly.MaxValue" /> or underrun
    /// <see cref="DateOnly.MinValue" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AddWorkingDaysOverflowTestData))]
    public void AddWorkingDays_WhenApplyingDaysWouldOverrunRange_ShouldThrowExactly(DateOnly input, int days)
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.AddWorkingDays(service, days);
        });
    }
}
