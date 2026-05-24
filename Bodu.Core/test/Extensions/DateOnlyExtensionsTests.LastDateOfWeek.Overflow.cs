// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.LastDateOfWeek.Overflow.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.LastDateOfWeek(DateOnly, CultureInfo)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when projecting the date forward to the end of its week would exceed
    /// <see cref="DateOnly.MaxValue" />.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeek_DateOnly_WithCulture_WhenResultExceedsMaxValue_ShouldThrowExactly()
    {
        // DateOnly.MaxValue (9999-12-31) is a Friday; an en-US culture treats Sunday as week start,
        // making Saturday the last day of the week. Projecting forward by one day overflows.
        DateOnly date = DateOnly.MaxValue;
        var culture = CultureInfo.GetCultureInfo("en-US");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = date.LastDateOfWeek(culture);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.LastDateOfWeek(DateOnly, WorkingDaysOfWeek)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when projecting forward would exceed <see cref="DateOnly.MaxValue" />.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeek_DateOnly_WithWeekendDefinition_WhenResultExceedsMaxValue_ShouldThrowExactly()
    {
        // With SaturdaySunday weekend, the week starts on Monday and ends on Sunday.
        // DateOnly.MaxValue is Friday, so end-of-week is Sunday — two days past MaxValue, which overflows.
        DateOnly date = DateOnly.MaxValue;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = date.LastDateOfWeek(WorkingDaysOfWeek.MondayToFriday);
        });
    }

}
