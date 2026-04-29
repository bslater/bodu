// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.EnumerateNonWorkingDays.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the enumeration yields the Saturday and Sunday inside a one-week range.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_WhenOneWeekRange_ShouldYieldWeekend()
    {
        NotableDateService service = BuildService();

        DateOnly[] result = new DateOnly(2026, 1, 5).EnumerateNonWorkingDays(new DateOnly(2026, 1, 11), service).ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 11) },
            result);
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" /> eagerly.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_WhenServiceIsNull_ShouldThrowArgumentNullExceptionEagerly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 5).EnumerateNonWorkingDays(new DateOnly(2026, 1, 11), service: null!);
        });
    }
}
