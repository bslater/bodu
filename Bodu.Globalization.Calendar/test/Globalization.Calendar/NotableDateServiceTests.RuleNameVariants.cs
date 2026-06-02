// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.RuleNameVariants.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that rules sharing a canonical name but differing by rule-level variant coexist and both resolve through
/// the full service pipeline.
/// </summary>
public sealed partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that two rules sharing a canonical name but differing by rule-level variant both resolve for a year,
    /// rather than collapsing under a name-only identity.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenVariantsShareCanonicalName_ShouldResolveBoth()
    {
        NotableDateService service = BuildService(
            Variant("Founders Day", "historic", 1, 10),
            Variant("Founders Day", "modern", 7, 20));

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2026);

        var foundersDays = results.Where(d => d.Name == "Founders Day").Select(d => d.Date).ToList();

        Assert.AreEqual(2, foundersDays.Count);
        CollectionAssert.AreEquivalent(
            new[] { new DateTime(2026, 1, 10), new DateTime(2026, 7, 20) },
            foundersDays);
    }

    /// <summary>
    /// Creates a fixed-date <see cref="NotableDateRule" /> carrying a rule-level variant name.
    /// </summary>
    /// <param name="name">The canonical name.</param>
    /// <param name="ruleName">The rule-level variant identifier.</param>
    /// <param name="month">The fixed month.</param>
    /// <param name="day">The fixed day.</param>
    /// <returns>The constructed rule.</returns>
    private static NotableDateRule Variant(string name, string ruleName, int month, int day) =>
        new()
        {
            Name = name,
            RuleName = ruleName,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Civic,
            Month = month,
            Day = day,
        };
}
