// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchorRelativeRuleIndexTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests the <see cref="AnchorRelativeRuleIndex" /> class.
/// </summary>
[TestClass]
public sealed class AnchorRelativeRuleIndexTests
{
    /// <summary>
    /// Verifies that rules whose offsets fall inside the inclusive offset range are returned.
    /// </summary>
    [TestMethod]
    public void FindRules_WhenOffsetsFallInsideRange_ShouldReturnMatchingRules()
    {
        NotableDateRule startOfLent = OffsetRule("Start of Lent", "Easter Sunday", -46);
        NotableDateRule palmSunday = OffsetRule("Palm Sunday", "Easter Sunday", -7);
        NotableDateRule goodFriday = OffsetRule("Good Friday", "Easter Sunday", -2);
        NotableDateRule easterMonday = OffsetRule("Easter Monday", "Easter Sunday", 1);

        AnchorRelativeRuleIndex index = new(
        [
            startOfLent,
            palmSunday,
            goodFriday,
            easterMonday,
        ]);

        IReadOnlyList<NotableDateRule> actual = index.FindRules(
            "Easter Sunday",
            minOffsetDays: -50,
            maxOffsetDays: -40);

        CollectionAssert.AreEqual(
            new[] { startOfLent },
            actual.ToArray());
    }

    /// <summary>
    /// Verifies that the offset range is inclusive at both ends.
    /// </summary>
    [TestMethod]
    public void FindRules_WhenOffsetsEqualRangeBoundaries_ShouldIncludeBoundaryRules()
    {
        NotableDateRule startOfLent = OffsetRule("Start of Lent", "Easter Sunday", -46);
        NotableDateRule palmSunday = OffsetRule("Palm Sunday", "Easter Sunday", -7);

        AnchorRelativeRuleIndex index = new(
        [
            startOfLent,
            palmSunday,
        ]);

        IReadOnlyList<NotableDateRule> actual = index.FindRules(
            "Easter Sunday",
            minOffsetDays: -46,
            maxOffsetDays: -7);

        CollectionAssert.AreEqual(
            new[] { startOfLent, palmSunday },
            actual.ToArray());
    }

    /// <summary>
    /// Verifies that anchor names are matched case-insensitively.
    /// </summary>
    [TestMethod]
    public void FindRules_WhenAnchorNameUsesDifferentCasing_ShouldReturnMatchingRules()
    {
        NotableDateRule startOfLent = OffsetRule("Start of Lent", "Easter Sunday", -46);

        AnchorRelativeRuleIndex index = new([startOfLent]);

        IReadOnlyList<NotableDateRule> actual = index.FindRules(
            "easter sunday",
            minOffsetDays: -46,
            maxOffsetDays: -46);

        CollectionAssert.AreEqual(
            new[] { startOfLent },
            actual.ToArray());
    }

    /// <summary>
    /// Verifies that rules for other anchors are excluded.
    /// </summary>
    [TestMethod]
    public void FindRules_WhenRulesUseDifferentAnchor_ShouldExcludeOtherAnchorRules()
    {
        NotableDateRule startOfLent = OffsetRule("Start of Lent", "Easter Sunday", -46);
        NotableDateRule orthodoxLent = OffsetRule("Orthodox Start of Lent", "Orthodox Easter Sunday", -48);

        AnchorRelativeRuleIndex index = new(
        [
            startOfLent,
            orthodoxLent,
        ]);

        IReadOnlyList<NotableDateRule> actual = index.FindRules(
            "Easter Sunday",
            minOffsetDays: -100,
            maxOffsetDays: 100);

        CollectionAssert.AreEqual(
            new[] { startOfLent },
            actual.ToArray());
    }

    /// <summary>
    /// Verifies that unsupported or incomplete rules are not indexed.
    /// </summary>
    [TestMethod]
    public void FindRules_WhenRulesAreNotAnchorRelative_ShouldIgnoreThem()
    {
        NotableDateRule fixedRule = new()
        {
            Name = "Christmas Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Religious,
            Month = 12,
            Day = 25,
        };

        NotableDateRule missingAnchor = new()
        {
            Name = "Incomplete Offset Rule",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Religious,
            OffsetDays = -46,
        };

        NotableDateRule missingOffset = new()
        {
            Name = "Another Incomplete Offset Rule",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Religious,
            AnchorRuleName = "Easter Sunday",
        };

        AnchorRelativeRuleIndex index = new(
        [
            fixedRule,
            missingAnchor,
            missingOffset,
        ]);

        IReadOnlyList<NotableDateRule> actual = index.FindRules(
            "Easter Sunday",
            minOffsetDays: -100,
            maxOffsetDays: 100);

        Assert.AreEqual(0, actual.Count);
    }

    /// <summary>
    /// Verifies that an empty result is returned when the offset range is invalid.
    /// </summary>
    [TestMethod]
    public void FindRules_WhenMinimumOffsetIsGreaterThanMaximumOffset_ShouldReturnEmptyResult()
    {
        NotableDateRule startOfLent = OffsetRule("Start of Lent", "Easter Sunday", -46);

        AnchorRelativeRuleIndex index = new([startOfLent]);

        IReadOnlyList<NotableDateRule> actual = index.FindRules(
            "Easter Sunday",
            minOffsetDays: 10,
            maxOffsetDays: -10);

        Assert.AreEqual(0, actual.Count);
    }

    /// <summary>
    /// Verifies that an empty result is returned when the anchor has no indexed rules.
    /// </summary>
    [TestMethod]
    public void FindRules_WhenAnchorHasNoRules_ShouldReturnEmptyResult()
    {
        NotableDateRule startOfLent = OffsetRule("Start of Lent", "Easter Sunday", -46);

        AnchorRelativeRuleIndex index = new([startOfLent]);

        IReadOnlyList<NotableDateRule> actual = index.FindRules(
            "Missing Anchor",
            minOffsetDays: -100,
            maxOffsetDays: 100);

        Assert.AreEqual(0, actual.Count);
    }

    /// <summary>
    /// Verifies that matching rules are returned in deterministic offset, priority, and name order.
    /// </summary>
    [TestMethod]
    public void FindRules_WhenMultipleRulesMatch_ShouldReturnRulesInDeterministicOrder()
    {
        NotableDateRule laterByName = OffsetRule("Rule B", "Easter Sunday", -10) with
        {
            Priority = 20,
        };

        NotableDateRule earlierByName = OffsetRule("Rule A", "Easter Sunday", -10) with
        {
            Priority = 20,
        };

        NotableDateRule earlierByPriority = OffsetRule("Rule C", "Easter Sunday", -10) with
        {
            Priority = 10,
        };

        NotableDateRule earlierByOffset = OffsetRule("Rule D", "Easter Sunday", -20) with
        {
            Priority = 100,
        };

        AnchorRelativeRuleIndex index = new(
        [
            laterByName,
            earlierByName,
            earlierByPriority,
            earlierByOffset,
        ]);

        IReadOnlyList<NotableDateRule> actual = index.FindRules(
            "Easter Sunday",
            minOffsetDays: -20,
            maxOffsetDays: -10);

        CollectionAssert.AreEqual(
            new[]
            {
                earlierByOffset,
                earlierByPriority,
                earlierByName,
                laterByName,
            },
            actual.ToArray());
    }

    /// <summary>
    /// Verifies that invalid anchor names fail before lookup.
    /// </summary>
    /// <param name="anchorRuleName">The invalid anchor rule name.</param>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void FindRules_WhenAnchorRuleNameIsNullOrWhiteSpace_ShouldThrowExactly(string? anchorRuleName)
    {
        AnchorRelativeRuleIndex index = new([]);

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            index.FindRules(anchorRuleName!, minOffsetDays: -10, maxOffsetDays: 10);
        });

        Assert.AreEqual("anchorRuleName", ex.ParamName);
    }

    /// <summary>
    /// Creates an offset-from-anchor notable-date rule.
    /// </summary>
    /// <param name="name">The rule name.</param>
    /// <param name="anchorRuleName">The anchor rule name.</param>
    /// <param name="offsetDays">The offset from the anchor, in days.</param>
    /// <returns>The created rule.</returns>
    private static NotableDateRule OffsetRule(string name, string anchorRuleName, int offsetDays) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Religious,
            AnchorRuleName = anchorRuleName,
            OffsetDays = offsetDays,
        };
}