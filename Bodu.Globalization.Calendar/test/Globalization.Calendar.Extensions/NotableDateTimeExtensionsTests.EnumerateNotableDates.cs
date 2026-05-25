// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.EnumerateNotableDates.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that the enumeration yields every notable date intersecting the inclusive range.
    /// </summary>
    [TestMethod]
    public void EnumerateNotableDates_WhenRangeContainsRules_ShouldYieldMatchingDates()
    {
        NotableDateService service = BuildService(
            Fixed("New Year's Day", 1, 1),
            Fixed("Anzac Day", 4, 25),
            Fixed("Christmas Day", 12, 25));

        NotableDate[] result = new DateTime(2026, 4, 1).EnumerateNotableDates(new DateTime(2026, 12, 31), service).ToArray();

        Assert.AreEqual(2, result.Length);
        Assert.AreEqual("Anzac Day", result[0].Name);
        Assert.AreEqual("Christmas Day", result[1].Name);
    }

    /// <summary>
    /// Verifies that a filter restricts the enumeration to matching notable dates only.
    /// </summary>
    [TestMethod]
    public void EnumerateNotableDates_WhenFilterApplied_ShouldYieldOnlyMatchingDates()
    {
        NotableDateService service = BuildService(
            Fixed("Festival", 4, 1, NotableDateCategory.Cultural),
            Fixed("Christmas Day", 12, 25, NotableDateCategory.Holiday));
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);

        NotableDate[] result = new DateTime(2026, 1, 1).EnumerateNotableDates(new DateTime(2026, 12, 31), service, filter).ToArray();

        Assert.AreEqual(1, result.Length);
        Assert.AreEqual("Christmas Day", result[0].Name);
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void EnumerateNotableDates_WhenServiceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 1).EnumerateNotableDates(new DateTime(2026, 12, 31), service: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> filter to the explicit-service overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void EnumerateNotableDates_WhenExplicitFilterIsNull_ShouldThrowExactly()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 1).EnumerateNotableDates(new DateTime(2026, 12, 31), service, filter: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> filter to the ambient overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void EnumerateNotableDates_WhenAmbientFilterIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 1).EnumerateNotableDates(new DateTime(2026, 12, 31), filter: null!);
        });
    }

    /// <summary>
    /// Verifies that swapping the start and end boundaries throws under the strict reversed-range policy applied across
    /// every range-accepting public API. Callers must supply a well-ordered range.
    /// </summary>
    [TestMethod]
    public void EnumerateNotableDates_WhenBoundariesReversed_ShouldThrow()
    {
        NotableDateService service = BuildService(
            Fixed("New Year's Day", 1, 1),
            Fixed("Christmas Day", 12, 25));

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new DateTime(2026, 12, 31).EnumerateNotableDates(new DateTime(2026, 1, 1), service).ToArray();
        });
    }
}
