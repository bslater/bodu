// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.PreviousDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousDateOfWeek(DateTime, DayOfWeek)" /> returns the prior occurrence of the requested <see cref="DayOfWeek" /> for each <c>(input, target)</c> pair.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(PreviousDateOfWeekTestData))]
    public void PreviousDateOfWeek_WhenCalled_ShouldReturnExpectedDate(DateTime input, DayOfWeek targetDay, DateTime expected)
    {
        DateTime actual = input.PreviousDateOfWeek(targetDay);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void PreviousDateOfWeek_WhenEnumIsInvalid_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 4, 18);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.PreviousDateOfWeek((DayOfWeek)999);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousDateOfWeek(DateTime, DayOfWeek)" /> preserves the input's <see cref="DateTime.Kind" /> across all <see cref="DateTimeKind" /> values.
    /// </summary>
    [TestMethod]
    [DataRow(DateTimeKind.Unspecified)]
    [DataRow(DateTimeKind.Utc)]
    [DataRow(DateTimeKind.Local)]
    public void PreviousDateOfWeek_WhenKindIsSet_ShouldPreserveKind(DateTimeKind kind)
    {
        DateTime input = new DateTime(2024, 4, 18, 10, 0, 0, kind);
        DateTime actual = input.PreviousDateOfWeek(DayOfWeek.Wednesday);

        Assert.AreEqual(kind, actual.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousDateOfWeek(DateTime, DayOfWeek)" /> preserves a sub-second-precision <see cref="DateTime.TimeOfDay" /> on the resulting date.
    /// </summary>
    [TestMethod]
    public void PreviousDateOfWeek_WhenTimeIsSet_ShouldPreserveTimed()
    {
        var time = new TimeSpan(0, 12, 32, 55, 34, 903);
        var input = new DateTime(2024, 4, 18).Add(time);

        var actual = input.PreviousDateOfWeek(DayOfWeek.Monday).TimeOfDay;

        Assert.AreEqual(time, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTime.MaxValue" /> targeting Saturday returns a valid result on or before <see cref="DateTime.MaxValue" />.
    /// </summary>
    [TestMethod]
    public void PreviousDateOfWeek_WhenUsingMaxValue_ShouldSucceed()
    {
        var input = DateTime.MaxValue;
        var actual = input.PreviousDateOfWeek(DayOfWeek.Saturday);

        Assert.IsTrue(actual <= DateTime.MaxValue);
    }

    /// <summary>
    /// Verifies that <see cref="DateTime.MinValue" /> + 7 days returning the prior Monday yields a result on or after <see cref="DateTime.MinValue" />.
    /// </summary>
    [TestMethod]
    public void PreviousDateOfWeek_WhenUsingMinValue_ShouldReturnSameOrGreater()
    {
        var actual = DateTime.MinValue.AddDays(7).PreviousDateOfWeek(DayOfWeek.Monday);

        Assert.IsTrue(actual >= DateTime.MinValue);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousDateOfWeek(DateTime, DayOfWeek)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the <c>dateTime</c> argument when the occurrence precedes the
    /// first date a <see cref="DateTime" /> can represent.
    /// </summary>
    [TestMethod]
    public void PreviousDateOfWeek_WhenOccurrenceIsOutsideDateTimeRange_ShouldThrowArgumentOutOfRangeExceptionForDate()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTime.MinValue.PreviousDateOfWeek(DayOfWeek.Sunday);
        });

        Assert.AreEqual("dateTime", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousDateOfWeek(DateTime, DayOfWeek)" /> still returns an occurrence that falls on the
    /// first date a <see cref="DateTime" /> can represent.
    /// </summary>
    [TestMethod]
    public void PreviousDateOfWeek_WhenOccurrenceIsTheFirstRepresentableDateTime_ShouldReturnIt()
    {
        var actual = DateTime.MinValue.AddDays(7).PreviousDateOfWeek(DayOfWeek.Monday);

        Assert.AreEqual(DateTime.MinValue, actual);
    }
}
