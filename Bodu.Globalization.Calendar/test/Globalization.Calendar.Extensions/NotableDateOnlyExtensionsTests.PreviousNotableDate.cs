// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.PreviousNotableDate.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the previous notable date returned is the latest occurring strictly before the input.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenMultipleRulesExist_ShouldReturnLatestBeforeInput()
    {
        NotableDateService service = BuildService(
            Fixed("New Year's Day", 1, 1),
            Fixed("Anzac Day", 4, 25),
            Fixed("Christmas Day", 12, 25));

        NotableDate? result = new DateOnly(2026, 6, 15).PreviousNotableDate(service);

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateTime(2026, 4, 25), result.Date);
        Assert.AreEqual("Anzac Day", result.Name);
    }

    /// <summary>
    /// Verifies that when no rule fires earlier in the same year the search rolls into the previous year.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenNoRuleEarlierInYear_ShouldRollToPreviousYear()
    {
        NotableDateService service = BuildService(Fixed("Christmas Day", 12, 25));

        NotableDate? result = new DateOnly(2026, 6, 15).PreviousNotableDate(service);

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateTime(2025, 12, 25), result.Date);
    }

    /// <summary>
    /// Verifies that a filter restricts the search to matching notable dates only.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenFilterApplied_ShouldReturnLastMatchingDate()
    {
        NotableDateService service = BuildService(
            Fixed("Festival", 4, 1, NotableDateCategory.Cultural),
            Fixed("Christmas Day", 12, 25, NotableDateCategory.Holiday));
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);

        NotableDate? result = new DateOnly(2026, 6, 15).PreviousNotableDate(service, filter);

        Assert.IsNotNull(result);
        Assert.AreEqual("Christmas Day", result.Name);
        Assert.AreEqual(2025, result.Date.Year);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 15));
        try
        {
            NotableDateContext.Default = service;

            NotableDate? result = new DateOnly(2026, 6, 30).PreviousNotableDate();

            Assert.IsNotNull(result);
            Assert.AreEqual(new DateTime(2026, 1, 15), result.Date);
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
    public void PreviousNotableDate_WhenServiceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 6).PreviousNotableDate(service: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> filter to the explicit-service overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenExplicitFilterIsNull_ShouldThrowExactly()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 1).PreviousNotableDate(service, filter: null!);
        });
    }

    /// <summary>
    /// Verifies that an input matching a rule's anchor is treated as <i>not</i> previous; the prior occurrence is returned instead.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenInputMatchesAnchor_ShouldReturnPriorOccurrence()
    {
        NotableDateService service = BuildService(Fixed("New Year's Day", 1, 1));

        NotableDate? result = new DateOnly(2026, 1, 1).PreviousNotableDate(service);

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateTime(2025, 1, 1), result.Date);
    }

    /// <summary>
    /// Verifies that, with no rules configured, the search returns <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenNoRulesConfigured_ShouldReturnNull()
    {
        NotableDateService service = BuildService();

        NotableDate? result = new DateOnly(2026, 1, 1).PreviousNotableDate(service);

        Assert.IsNull(result);
    }

    /// <summary>
    /// Verifies that an input lying inside a multi-day span is treated as after the span's anchor; the search returns the same span.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenInputLiesInsideMultiDaySpan_ShouldReturnSpan()
    {
        NotableDateRule festival = Fixed("Festival", 6, 1) with { DurationDays = 5 };
        NotableDateService service = BuildService(festival);

        NotableDate? result = new DateOnly(2026, 6, 3).PreviousNotableDate(service);

        Assert.IsNotNull(result);
        Assert.AreEqual("Festival", result.Name);
        Assert.AreEqual(new DateTime(2026, 6, 1), result.Date);
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> filter to the ambient overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenAmbientFilterIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 1).PreviousNotableDate(filter: null!);
        });
    }
}
