// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.NextNotableDate.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that the next notable date returned is the earliest occurring strictly after the input.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenMultipleRulesExist_ShouldReturnEarliestAfterInput()
    {
        NotableDateService service = BuildService(
            Fixed("New Year's Day", 1, 1),
            Fixed("Anzac Day", 4, 25),
            Fixed("Christmas Day", 12, 25));

        NotableDate? result = new DateTime(2026, 6, 15).NextNotableDate(service);

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateTime(2026, 12, 25), result.Date);
        Assert.AreEqual("Christmas Day", result.Name);
    }

    /// <summary>
    /// Verifies that when no rule fires later in the same year the search rolls into the next year.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenNoRuleLaterInYear_ShouldRollToNextYear()
    {
        NotableDateService service = BuildService(Fixed("New Year's Day", 1, 1));

        NotableDate? result = new DateTime(2026, 6, 15).NextNotableDate(service);

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateTime(2027, 1, 1), result.Date);
    }

    /// <summary>
    /// Verifies that an input date matching an anchor day is treated as <i>not</i> next; the following occurrence is returned instead.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenInputMatchesAnchor_ShouldReturnFollowingOccurrence()
    {
        NotableDateService service = BuildService(Fixed("New Year's Day", 1, 1));

        NotableDate? result = new DateTime(2026, 1, 1).NextNotableDate(service);

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateTime(2027, 1, 1), result.Date);
    }

    /// <summary>
    /// Verifies that a filter restricts the search to matching notable dates only.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenFilterApplied_ShouldReturnFirstMatchingDate()
    {
        NotableDateService service = BuildService(
            Fixed("Festival", 4, 1, NotableDateCategory.Cultural),
            Fixed("Christmas Day", 12, 25, NotableDateCategory.Holiday));
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);

        NotableDate? result = new DateTime(2026, 1, 15).NextNotableDate(service, filter);

        Assert.IsNotNull(result);
        Assert.AreEqual("Christmas Day", result.Name);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 6, 30));
        try
        {
            NotableDateContext.Default = service;

            NotableDate? result = new DateTime(2026, 1, 1).NextNotableDate();

            Assert.IsNotNull(result);
            Assert.AreEqual(new DateTime(2026, 6, 30), result.Date);
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
    public void NextNotableDate_WhenServiceIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 6).NextNotableDate(service: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> filter to the explicit-service overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenExplicitFilterIsNull_ShouldThrowArgumentNullException()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 1).NextNotableDate(service, filter: null!);
        });
    }
}
