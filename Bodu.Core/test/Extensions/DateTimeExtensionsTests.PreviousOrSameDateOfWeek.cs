// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.PreviousOrSameDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousOrSameDateOfWeek(DateTime, DayOfWeek)" /> returns the prior
    /// on-or-before occurrence of the requested <see cref="DayOfWeek" /> for each <c>(input, target)</c> pair.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(PreviousOrSameDateOfWeekTestData))]
    public void PreviousOrSameDateOfWeek_WhenCalled_ShouldReturnExpectedDate(DateTime input, DayOfWeek targetDay, DateTime expected)
    {
        DateTime actual = input.PreviousOrSameDateOfWeek(targetDay);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void PreviousOrSameDateOfWeek_WhenEnumIsInvalid_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 4, 18);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.PreviousOrSameDateOfWeek((DayOfWeek)999);
        });
    }

    /// <summary>
    /// Verifies that targeting the input's own <see cref="DateTime.DayOfWeek" /> returns the input itself, distinguishing
    /// the on-or-same variant from the strict <see cref="DateTimeExtensions.PreviousDateOfWeek(DateTime, DayOfWeek)" />.
    /// </summary>
    [TestMethod]
    public void PreviousOrSameDateOfWeek_WhenAlreadyOnTargetDay_ShouldReturnInputUnchanged()
    {
        var input = new DateTime(2024, 4, 18, 17, 45, 00); // Thursday
        DateTime actual = input.PreviousOrSameDateOfWeek(DayOfWeek.Thursday);

        Assert.AreEqual(input, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousOrSameDateOfWeek(DateTime, DayOfWeek)" /> preserves the
    /// input's <see cref="DateTime.Kind" /> across all <see cref="DateTimeKind" /> values.
    /// </summary>
    [TestMethod]
    [DataRow(DateTimeKind.Unspecified)]
    [DataRow(DateTimeKind.Utc)]
    [DataRow(DateTimeKind.Local)]
    public void PreviousOrSameDateOfWeek_WhenKindIsSet_ShouldPreserveKind(DateTimeKind kind)
    {
        var input = new DateTime(2024, 4, 18, 10, 0, 0, kind);
        DateTime actual = input.PreviousOrSameDateOfWeek(DayOfWeek.Wednesday);

        Assert.AreEqual(kind, actual.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousOrSameDateOfWeek(DateTime, DayOfWeek)" /> preserves a
    /// sub-second-precision <see cref="DateTime.TimeOfDay" /> on the resulting date.
    /// </summary>
    [TestMethod]
    public void PreviousOrSameDateOfWeek_WhenTimeIsSet_ShouldPreserveTimed()
    {
        var time = new TimeSpan(0, 12, 32, 55, 34, 903);
        var input = new DateTime(2024, 4, 18).Add(time);

        var actual = input.PreviousOrSameDateOfWeek(DayOfWeek.Monday).TimeOfDay;

        Assert.AreEqual(time, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTime.MaxValue" /> targeting Saturday returns a valid result on or before
    /// <see cref="DateTime.MaxValue" />.
    /// </summary>
    [TestMethod]
    public void PreviousOrSameDateOfWeek_WhenUsingMaxValue_ShouldSucceed()
    {
        var input = DateTime.MaxValue;
        var actual = input.PreviousOrSameDateOfWeek(DayOfWeek.Saturday);

        Assert.IsLessThanOrEqualTo(DateTime.MaxValue, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTime.MinValue" /> targeting its own <see cref="DateTime.DayOfWeek" /> returns the
    /// input itself (no underflow).
    /// </summary>
    [TestMethod]
    public void PreviousOrSameDateOfWeek_WhenUsingMinValue_ShouldReturnSameOrGreater()
    {
        DateTime input = DateTime.MinValue;
        var actual = input.PreviousOrSameDateOfWeek(input.DayOfWeek);

        Assert.AreEqual(input, actual);
    }
}
