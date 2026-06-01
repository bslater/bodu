// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.AddWorkingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that a positive number of days advances forward, equivalent to <see cref="NotableDateTimeExtensions.NextWorkingDay(DateTime, INotableDateService, int, string?, Type?)" />.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenPositiveDays_ShouldAdvanceForward()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 5).AddWorkingDays(service, days: 3);

        Assert.AreEqual(new DateTime(2026, 1, 8), result);
    }

    /// <summary>
    /// Verifies that a negative number of days retreats backward, equivalent to <see cref="NotableDateTimeExtensions.PreviousWorkingDay(DateTime, INotableDateService, int, string?, Type?)" />.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenNegativeDays_ShouldRetreatBackward()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 8).AddWorkingDays(service, days: -3);

        Assert.AreEqual(new DateTime(2026, 1, 5), result);
    }

    /// <summary>
    /// Verifies that zero days returns the input unchanged regardless of whether it is a working day.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenZeroDays_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        var weekend = new DateTime(2026, 1, 3, 9, 0, 0, DateTimeKind.Utc);

        DateTime result = weekend.AddWorkingDays(service, days: 0);

        Assert.AreEqual(weekend, result);
        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
    }

    /// <summary>
    /// Verifies that zero days returns the input unchanged even when the input matches a non-working rule.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenZeroDaysOnNonWorkingRuleDay_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 1, nonWorking: true));
        var input = new DateTime(2026, 1, 1);

        DateTime result = input.AddWorkingDays(service, days: 0);

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

            DateTime result = new DateTime(2026, 1, 5).AddWorkingDays(2);

            Assert.AreEqual(new DateTime(2026, 1, 7), result);
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
            _ = new DateTime(2026, 1, 6).AddWorkingDays(service: null!, days: 1);
        });
    }

    /// <summary>
    /// Verifies that signed-day arithmetic produces the expected working-day result across positive, negative and zero values.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AddWorkingDaysSignedTestData))]
    public void AddWorkingDays_WhenSignedDaysSupplied_ShouldReturnExpectedWorkingDay(DateTime input, int days, DateTime expected)
    {
        NotableDateService service = BuildService();

        DateTime actual = input.AddWorkingDays(service, days);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that adding working days that would overrun <see cref="DateTime.MaxValue" /> or underrun
    /// <see cref="DateTime.MinValue" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AddWorkingDaysOverflowTestData))]
    public void AddWorkingDays_WhenApplyingDaysWouldOverrunRange_ShouldThrowExactly(DateTime input, int days)
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.AddWorkingDays(service, days);
        });
    }

    /// <summary>
    /// Verifies that the returned <see cref="DateTime" /> preserves the input <see cref="DateTime.Kind" /> and time-of-day across each
    /// supported <see cref="DateTimeKind" /> value, including for the zero-days short-circuit and the directional walk paths.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeKindPreservationTestData))]
    public void AddWorkingDays_WhenCalled_ShouldPreserveKindAndTimeOfDay(DateTimeKind kind)
    {
        NotableDateService service = BuildService();
        var input = new DateTime(2026, 1, 6, 11, 22, 33, kind);

        DateTime forward = input.AddWorkingDays(service, days: 2);
        DateTime backward = input.AddWorkingDays(service, days: -2);
        DateTime same = input.AddWorkingDays(service, days: 0);

        Assert.AreEqual(kind, forward.Kind);
        Assert.AreEqual(new TimeSpan(11, 22, 33), forward.TimeOfDay);
        Assert.AreEqual(kind, backward.Kind);
        Assert.AreEqual(new TimeSpan(11, 22, 33), backward.TimeOfDay);
        Assert.AreEqual(kind, same.Kind);
        Assert.AreEqual(new TimeSpan(11, 22, 33), same.TimeOfDay);
    }
}
