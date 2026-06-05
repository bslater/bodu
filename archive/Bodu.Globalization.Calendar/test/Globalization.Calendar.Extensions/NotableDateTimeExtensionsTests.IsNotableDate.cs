// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.IsNotableDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that a day with no matching notable-date rule is reported as not notable.
    /// </summary>
    [TestMethod]
    public void IsNotableDate_WhenDayHasNoRule_ShouldReturnFalse()
    {
        NotableDateService service = BuildService();

        var result = new DateTime(2026, 6, 15).IsNotableDate(service);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a day matching a single-day rule is reported as notable.
    /// </summary>
    [TestMethod]
    public void IsNotableDate_WhenDayMatchesSingleDayRule_ShouldReturnTrue()
    {
        NotableDateService service = BuildService(Fixed("New Year's Day", 1, 1));

        var result = new DateTime(2026, 1, 1).IsNotableDate(service);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a day inside a multi-day span is reported as notable even though the span anchor lies on a previous day.
    /// </summary>
    [TestMethod]
    public void IsNotableDate_WhenDayLiesInsideMultiDaySpan_ShouldReturnTrue()
    {
        NotableDateRule rule = Fixed("Festival", 6, 1) with { DurationDays = 5 };
        NotableDateService service = BuildService(rule);

        var result = new DateTime(2026, 6, 3).IsNotableDate(service);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that supplying a filter that matches the rule yields <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void IsNotableDate_WhenFilterMatches_ShouldReturnTrue()
    {
        NotableDateService service = BuildService(Fixed("Christmas Day", 12, 25, NotableDateCategory.Holiday));
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);

        var result = new DateTime(2026, 12, 25).IsNotableDate(service, filter);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that supplying a filter that excludes the rule yields <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void IsNotableDate_WhenFilterExcludes_ShouldReturnFalse()
    {
        NotableDateService service = BuildService(Fixed("Christmas Day", 12, 25, NotableDateCategory.Holiday));
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Cultural);

        var result = new DateTime(2026, 12, 25).IsNotableDate(service, filter);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void IsNotableDate_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 8, 15));
        try
        {
            NotableDateContext.Default = service;

            var result = new DateTime(2026, 8, 15).IsNotableDate();

            Assert.IsTrue(result);
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
    public void IsNotableDate_WhenServiceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 1).IsNotableDate(service: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> filter to the ambient overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void IsNotableDate_WhenAmbientFilterIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 1).IsNotableDate(filter: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> filter to the explicit-service overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void IsNotableDate_WhenExplicitFilterIsNull_ShouldThrowExactly()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 1).IsNotableDate(service, filter: null!);
        });
    }
}
