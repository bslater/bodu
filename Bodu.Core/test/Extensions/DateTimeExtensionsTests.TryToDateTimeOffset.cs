// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.TryToDateTimeOffset.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>Verifies that valid whole-minute offsets preserve the wall-clock time and requested offset.</summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(2, 30)]
    [DataRow(-3, 0)]
    [DataRow(13, 45)]
    [DataRow(14, 0)]
    [DataRow(-14, 0)]
    public void TryToDateTimeOffset_WithValidOffset_ShouldReturnExpected(int hours, int minutes)
    {
        var input = new DateTime(2024, 4, 18, 12, 30, 20, DateTimeKind.Unspecified).AddTicks(1234);
        var offset = new TimeSpan(hours, minutes, 0);
        bool parsed = input.TryToDateTimeOffset(offset, out DateTimeOffset actual);
        Assert.IsTrue(parsed);
        Assert.AreEqual(input, actual.DateTime);
        Assert.AreEqual(offset, actual.Offset);
        Assert.AreEqual(new DateTimeOffset(input, offset), actual);
    }

    /// <summary>Verifies that offsets outside the DateTimeOffset range fail without throwing.</summary>
    [TestMethod]
    [DataRow(15, 0)]
    [DataRow(-15, 0)]
    [DataRow(14, 1)]
    [DataRow(-14, -1)]
    public void TryToDateTimeOffset_WithOffsetOutsideRange_ShouldReturnFalse(int hours, int minutes)
    {
        var input = new DateTime(2024, 4, 18, 12, 0, 0, DateTimeKind.Unspecified);
        TimeSpan offset = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);
        bool parsed = input.TryToDateTimeOffset(offset, out DateTimeOffset actual);
        Assert.IsFalse(parsed);
        Assert.AreEqual(default(DateTimeOffset), actual);
    }

    /// <summary>Verifies that an offset with sub-minute precision is not accepted.</summary>
    [TestMethod]
    public void TryToDateTimeOffset_WhenOffsetContainsSeconds_ShouldReturnFalse()
    {
        DateTime input = new DateTime(2024, 4, 18, 12, 0, 0, DateTimeKind.Unspecified);
        Assert.IsFalse(input.TryToDateTimeOffset(TimeSpan.FromSeconds(90), out DateTimeOffset result));
        Assert.AreEqual(default(DateTimeOffset), result);
    }

    /// <summary>Verifies UTC boundary underflow is reported without an exception.</summary>
    [TestMethod]
    public void TryToDateTimeOffset_WhenResultTooEarly_ShouldReturnFalse()
    {
        DateTime input = DateTime.SpecifyKind(DateTime.MinValue.AddHours(1), DateTimeKind.Unspecified);
        Assert.IsFalse(input.TryToDateTimeOffset(TimeSpan.FromHours(2), out DateTimeOffset result));
        Assert.AreEqual(default(DateTimeOffset), result);
    }

    /// <summary>Verifies UTC boundary overflow is reported without an exception.</summary>
    [TestMethod]
    public void TryToDateTimeOffset_WhenResultTooLate_ShouldReturnFalse()
    {
        DateTime input = DateTime.SpecifyKind(DateTime.MaxValue.AddHours(-1), DateTimeKind.Unspecified);
        Assert.IsFalse(input.TryToDateTimeOffset(TimeSpan.FromHours(-2), out DateTimeOffset result));
        Assert.AreEqual(default(DateTimeOffset), result);
    }

    /// <summary>Verifies that UTC inputs require a zero offset.</summary>
    [TestMethod]
    public void TryToDateTimeOffset_WhenKindIsUtc_ShouldRequireZeroOffset()
    {
        var input = new DateTime(2024, 4, 18, 12, 0, 0, DateTimeKind.Utc);
        Assert.IsTrue(input.TryToDateTimeOffset(TimeSpan.Zero, out DateTimeOffset zero));
        Assert.AreEqual(input, zero.UtcDateTime);
        Assert.IsFalse(input.TryToDateTimeOffset(TimeSpan.FromHours(1), out DateTimeOffset other));
        Assert.AreEqual(default(DateTimeOffset), other);
    }

    /// <summary>Verifies that Local inputs require the machine-local offset for the input date.</summary>
    [TestMethod]
    public void TryToDateTimeOffset_WhenKindIsLocal_ShouldRequireLocalOffset()
    {
        var input = new DateTime(2024, 4, 18, 12, 0, 0, DateTimeKind.Local);
        TimeSpan localOffset = TimeZoneInfo.Local.GetUtcOffset(input);
        Assert.IsTrue(input.TryToDateTimeOffset(localOffset, out DateTimeOffset valid));
        Assert.AreEqual(localOffset, valid.Offset);
        TimeSpan incompatibleOffset = localOffset == TimeSpan.Zero ? TimeSpan.FromHours(1) : TimeSpan.Zero;
        Assert.IsFalse(input.TryToDateTimeOffset(incompatibleOffset, out DateTimeOffset invalid));
        Assert.AreEqual(default(DateTimeOffset), invalid);
    }

    /// <summary>
    /// Verifies that a conversion that fails, under each rule the conversion applies, reports <see langword="false" />
    /// without throwing and catching an exception internally, so it allocates nothing.
    /// </summary>
    [TestMethod]
    public void TryToDateTimeOffset_WhenConversionFails_ShouldNotAllocate()
    {
        DateTime late = DateTime.SpecifyKind(DateTime.MaxValue.AddHours(-1), DateTimeKind.Unspecified);
        DateTime early = DateTime.SpecifyKind(DateTime.MinValue.AddHours(1), DateTimeKind.Unspecified);
        var unspecified = new DateTime(2024, 4, 18, 12, 0, 0, DateTimeKind.Unspecified);
        var utc = new DateTime(2024, 4, 18, 12, 0, 0, DateTimeKind.Utc);
        var local = new DateTime(2024, 4, 18, 12, 0, 0, DateTimeKind.Local);
        TimeSpan localOffset = TimeZoneInfo.Local.GetUtcOffset(local);
        TimeSpan otherOffset = localOffset == TimeSpan.Zero ? TimeSpan.FromHours(1) : TimeSpan.Zero;

        int CountFailures() =>
            (late.TryToDateTimeOffset(TimeSpan.FromHours(-2), out _) ? 0 : 1)
            + (early.TryToDateTimeOffset(TimeSpan.FromHours(2), out _) ? 0 : 1)
            + (unspecified.TryToDateTimeOffset(TimeSpan.FromSeconds(90), out _) ? 0 : 1)
            + (unspecified.TryToDateTimeOffset(TimeSpan.FromHours(15), out _) ? 0 : 1)
            + (utc.TryToDateTimeOffset(TimeSpan.FromHours(1), out _) ? 0 : 1)
            + (local.TryToDateTimeOffset(otherOffset, out _) ? 0 : 1);

        Assert.AreEqual(6, CountFailures());

        long before = GC.GetAllocatedBytesForCurrentThread();
        int failures = CountFailures();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(6, failures);
        Assert.AreEqual(0L, allocated, $"Six failed conversions allocated {allocated} bytes.");
    }
}
