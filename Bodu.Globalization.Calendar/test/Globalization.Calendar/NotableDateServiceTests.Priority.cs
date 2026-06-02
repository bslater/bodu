// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.Priority.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that a rule's priority is carried onto the emitted <see cref="NotableDate" /> (F-003).
/// </summary>
public sealed partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that a rule's <see cref="NotableDateRule.Priority" /> is propagated to the resolved
    /// <see cref="NotableDate.Priority" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenRulePriorityIsSet_ShouldCarryPriorityOntoNotableDate()
    {
        NotableDateRule rule = Fixed("Priority Day", 6, 1) with { Priority = 25 };
        NotableDateService service = BuildService(rule);

        NotableDate result = service.GetNotableDates(2026).Single(d => d.Name == "Priority Day");

        Assert.AreEqual(25, result.Priority);
    }

    /// <summary>
    /// Verifies that an unset rule priority surfaces as the default <see cref="NotableDate.Priority" /> of 100.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenRulePriorityNotSet_ShouldDefaultPriorityTo100()
    {
        NotableDateService service = BuildService(Fixed("Default Day", 6, 1));

        NotableDate result = service.GetNotableDates(2026).Single(d => d.Name == "Default Day");

        Assert.AreEqual(100, result.Priority);
    }

    /// <summary>
    /// Verifies that the default collision resolver orders same-day occurrences by ascending priority (lower value
    /// wins), independently of alphabetical name order.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenSameDayRulesHaveDifferentPriority_ShouldOrderByPriorityAscending()
    {
        // "Alpha" sorts before "Beta" alphabetically, but Beta has the lower (stronger) priority, so priority must win.
        NotableDateService service = BuildService(
            Fixed("Alpha", 6, 1, category: NotableDateCategory.Observance) with { Priority = 50 },
            Fixed("Beta", 6, 1, category: NotableDateCategory.Observance) with { Priority = 10 });

        var onDay = service.GetNotableDates(2026)
            .Where(d => d.Date == new DateTime(2026, 6, 1))
            .ToList();

        Assert.AreEqual(2, onDay.Count);
        Assert.AreEqual("Beta", onDay[0].Name, "The lower priority value (10) should sort first.");
        Assert.AreEqual("Alpha", onDay[1].Name);
    }
}
