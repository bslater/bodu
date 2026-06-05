// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.GetNotableDatesInYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that the year-scoped query returns every notable date for the input's year ordered chronologically.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInYear_WhenMultipleRulesExist_ShouldReturnAllDatesForYear()
    {
        NotableDateService service = BuildService(
            Fixed("New Year's Day", 1, 1),
            Fixed("Christmas Day", 12, 25));

        IReadOnlyList<NotableDate> result = new DateTime(2026, 6, 15).GetNotableDatesInYear(service);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(new DateTime(2026, 1, 1), result[0].Date);
        Assert.AreEqual(new DateTime(2026, 12, 25), result[1].Date);
    }

    /// <summary>
    /// Verifies that a filter restricts the returned set to matching notable dates only.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInYear_WhenFilterApplied_ShouldReturnOnlyMatching()
    {
        NotableDateService service = BuildService(
            Fixed("Festival", 4, 1, NotableDateCategory.Cultural),
            Fixed("Christmas Day", 12, 25, NotableDateCategory.Holiday));
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);

        IReadOnlyList<NotableDate> result = new DateTime(2026, 1, 1).GetNotableDatesInYear(service, filter);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Christmas Day", result[0].Name);
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInYear_WhenServiceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 1).GetNotableDatesInYear(service: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> filter to the explicit-service overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInYear_WhenExplicitFilterIsNull_ShouldThrowExactly()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 1).GetNotableDatesInYear(service, filter: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> filter to the ambient overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInYear_WhenAmbientFilterIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 1).GetNotableDatesInYear(filter: null!);
        });
    }
}
