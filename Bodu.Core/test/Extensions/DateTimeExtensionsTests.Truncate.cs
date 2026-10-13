// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.Truncate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    private static readonly DateTime Sample = new DateTime(2024, 4, 18, 14, 37, 56, 789).AddTicks(1234); // 7891234 ticks

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Truncate" />, when CalledWithResolution, returns the expected value.
    /// </summary>
    [TestMethod]
    [DataRow(DateTimeResolution.Year, "2024-01-01T00:00:00.0000000")]
    [DataRow(DateTimeResolution.Month, "2024-04-01T00:00:00.0000000")]
    [DataRow(DateTimeResolution.Day, "2024-04-18T00:00:00.0000000")]
    [DataRow(DateTimeResolution.Hour, "2024-04-18T14:00:00.0000000")]
    [DataRow(DateTimeResolution.Minute, "2024-04-18T14:37:00.0000000")]
    [DataRow(DateTimeResolution.Second, "2024-04-18T14:37:56.0000000")]
    [DataRow(DateTimeResolution.Millisecond, "2024-04-18T14:37:56.7890000")]
    [DataRow(DateTimeResolution.Tick, "2024-04-18T14:37:56.7891234")]
    public void Truncate_WhenCalledWithResolution_ShouldReturnExpected(DateTimeResolution resolution, string expectedDate)
    {
        var expected = DateTime.Parse(expectedDate);
        DateTime actual = Sample.Truncate(resolution);

        Assert.AreEqual(expected, actual);
        Assert.AreEqual(Sample.Kind, actual.Kind, "Kind should be preserved.");
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Truncate" />, when InputIsMaxValue, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Truncate_WhenInputIsMaxValue_ShouldNotThrow()
    {
        foreach (DateTimeResolution res in Enum.GetValues<DateTimeResolution>())
        {
            DateTime actual = DateTime.MaxValue.Truncate(res);

            Assert.IsTrue(actual <= DateTime.MaxValue, $"Failed at {res}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Truncate" />, when InputIsMinValue, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Truncate_WhenInputIsMinValue_ShouldNotThrow()
    {
        foreach (DateTimeResolution res in Enum.GetValues<DateTimeResolution>())
        {
            DateTime actual = DateTime.MinValue.Truncate(res);

            Assert.IsTrue(actual <= DateTime.MinValue.AddDays(1), $"Failed at {res}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Truncate" />, when KindIsPreserved, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Truncate_WhenKindIsPreserved_ShouldMatchOriginalKind()
    {
        var kinds = new[] { DateTimeKind.Utc, DateTimeKind.Local, DateTimeKind.Unspecified };
        foreach (var kind in kinds)
        {
            DateTime input = DateTime.SpecifyKind(Sample, kind);
            DateTime actual = input.Truncate(DateTimeResolution.Minute);

            Assert.AreEqual(kind, actual.Kind);
        }
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Truncate" />, when ResolutionIsInvalid, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Truncate_WhenResolutionIsInvalid_ShouldThrowExactly()
    {
        DateTimeResolution invalid = (DateTimeResolution)999;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Sample.Truncate(invalid);
        });
    }

    /// <summary>
    /// Verifies that a resolution just outside the range <see cref="DateTimeResolution" /> defines throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the <c>resolution</c> parameter.
    /// </summary>
    /// <param name="value">The undefined resolution value.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(8)]
    public void Truncate_WhenResolutionIsJustOutsideRange_ShouldThrowNamingParameter(int value)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Sample.Truncate((DateTimeResolution)value);
        });

        Assert.AreEqual("resolution", ex.ParamName);
    }

    /// <summary>
    /// Verifies that every defined <see cref="DateTimeResolution" /> is accepted and keeps the value's
    /// <see cref="DateTime.Kind" />.
    /// </summary>
    [TestMethod]
    public void Truncate_WhenEveryDefinedResolution_ShouldAcceptIt()
    {
        foreach (DateTimeResolution resolution in Enum.GetValues<DateTimeResolution>())
        {
            Assert.AreEqual(Sample.Kind, Sample.Truncate(resolution).Kind, resolution.ToString());
        }
    }
}
