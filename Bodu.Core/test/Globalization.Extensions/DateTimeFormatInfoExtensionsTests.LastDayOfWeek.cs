// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeFormatInfoExtensionsTests.LastDateOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Globalization.Extensions;

public partial class DateTimeFormatInfoExtensionsTests
{
    /// <summary>
    /// Provides (FirstDateOfWeek, expected LastDateOfWeek) pairs for the cyclic derivation
    /// <c>(FirstDateOfWeek + 6) mod 7</c>.
    /// </summary>
    public static IEnumerable<object[]> LastDateOfWeekTestData()
    {
        yield return new object[] { DayOfWeek.Sunday, DayOfWeek.Saturday };
        yield return new object[] { DayOfWeek.Monday, DayOfWeek.Sunday };
        yield return new object[] { DayOfWeek.Tuesday, DayOfWeek.Monday };
        yield return new object[] { DayOfWeek.Wednesday, DayOfWeek.Tuesday };
        yield return new object[] { DayOfWeek.Thursday, DayOfWeek.Wednesday };
        yield return new object[] { DayOfWeek.Friday, DayOfWeek.Thursday };
        yield return new object[] { DayOfWeek.Saturday, DayOfWeek.Friday };
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeFormatInfoExtensions.LastDateOfWeek" />, when FirstDateOfWeekIsSpecified, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LastDateOfWeekTestData))]
    public void LastDateOfWeek_WhenFirstDayOfWeekIsSpecified_ShouldReturnDaySixPositionsLater(
        DayOfWeek firstDayOfWeek, DayOfWeek expected)
    {
        DateTimeFormatInfo info = (DateTimeFormatInfo)CultureInfo.InvariantCulture.DateTimeFormat.Clone();
        info.FirstDayOfWeek = firstDayOfWeek;

        DayOfWeek actual = info.LastDayOfWeek();

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeFormatInfoExtensions.LastDateOfWeek" />, when UsingEnGbCulture, returns the expected value.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeek_WhenUsingEnGbCulture_ShouldReturnSunday()
    {
        DateTimeFormatInfo info = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat;

        DayOfWeek actual = info.LastDayOfWeek();

        Assert.AreEqual(DayOfWeek.Sunday, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeFormatInfoExtensions.LastDateOfWeek" />, when UsingEnUsCulture, returns the expected value.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeek_WhenUsingEnUsCulture_ShouldReturnSaturday()
    {
        DateTimeFormatInfo info = CultureInfo.GetCultureInfo("en-US").DateTimeFormat;

        DayOfWeek actual = info.LastDayOfWeek();

        Assert.AreEqual(DayOfWeek.Saturday, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeFormatInfoExtensions.LastDateOfWeek" />, when InfoIsNull, throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeek_WhenInfoIsNull_ShouldThrowArgumentNullException()
    {
        DateTimeFormatInfo? info = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = info!.LastDayOfWeek();
        });
    }
}
