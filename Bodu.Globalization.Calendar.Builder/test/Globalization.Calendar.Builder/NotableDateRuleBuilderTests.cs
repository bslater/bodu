// // ---------------------------------------------------------------------------------------------------------------
// // <copyright file="NotableDateRuleBuilderTests.cs" company="PlaceholderCompany">
// //     Copyright (c) PlaceholderCompany. All rights reserved.
// // </copyright>
// // ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Verifies the validation behaviour of <see cref="NotableDateRuleBuilder" />.
/// </summary>
[TestClass]
public class NotableDateRuleBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Fixed(int, int, bool, bool)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the month is less than 1.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenMonthIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.Fixed(0, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Fixed(int, int, bool, bool)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the month exceeds 13.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenMonthExceedsThirteen_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.Fixed(14, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Fixed(int, int, bool, bool)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the day is less than 1.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenDayIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.Fixed(1, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.DayOfWeekInMonth" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the month is less than 1.
    /// </summary>
    [TestMethod]
    public void DayOfWeekInMonth_WhenMonthIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.DayOfWeekInMonth(0, DayOfWeek.Monday, WeekOfMonthOrdinal.First);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.DayOfWeekInMonth" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the month exceeds 12.
    /// </summary>
    [TestMethod]
    public void DayOfWeekInMonth_WhenMonthExceedsTwelve_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.DayOfWeekInMonth(13, DayOfWeek.Monday, WeekOfMonthOrdinal.First);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.OffsetFromAnchor" /> throws <see cref="ArgumentNullException" />
    /// when the anchor rule name is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void OffsetFromAnchor_WhenAnchorRuleNameIsNull_ShouldThrowArgumentNullException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.OffsetFromAnchor(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Territory" /> throws an argument exception
    /// when the territory code is whitespace.
    /// </summary>
    [TestMethod]
    public void Territory_WhenCodeIsWhitespace_ShouldThrowArgumentException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.Territory("   ");
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Duration" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when the value is less than 1.
    /// </summary>
    [TestMethod]
    public void Duration_WhenLessThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.Duration(0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.OccurrenceYears" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when the value is less than 1.
    /// </summary>
    [TestMethod]
    public void OccurrenceYears_WhenLessThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.OccurrenceYears(0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.AddAdjustment" /> throws <see cref="ArgumentNullException" />
    /// when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddAdjustment_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddAdjustment(null!, _ => { });
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.AddAdjustment" /> throws <see cref="ArgumentNullException" />
    /// when the configure callback is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddAdjustment_WhenConfigureIsNull_ShouldThrowArgumentNullException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddAdjustment("key", null!);
        });
    }

    // ============================================================================
    // Strategy uniqueness — each builder commits to exactly one resolution strategy.
    // ============================================================================

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Fixed(int, int, bool, bool)" /> throws
    /// <see cref="InvalidOperationException" /> when any other strategy has already been selected on the
    /// builder, including a previous call to either Fixed overload.
    /// </summary>
    /// <param name="firstStrategy">The strategy name applied to the builder before the second Fixed call.</param>
    [TestMethod]
    [DataRow("Fixed")]
    [DataRow("DayOfWeekInMonth")]
    [DataRow("OffsetFromAnchor")]
    [DataRow("Algorithm")]
    public void Fixed_WhenStrategyAlreadySet_ForNumericMonth_ShouldThrowInvalidOperationException(string firstStrategy)
    {
        NotableDateRuleBuilder builder = NewBuilderWithStrategy(firstStrategy);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.Fixed(2, 2);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Fixed(string, int, bool, bool)" /> throws
    /// <see cref="InvalidOperationException" /> when any other strategy has already been selected on the
    /// builder, including a previous call to either Fixed overload.
    /// </summary>
    /// <param name="firstStrategy">The strategy name applied to the builder before the second Fixed call.</param>
    [TestMethod]
    [DataRow("Fixed")]
    [DataRow("DayOfWeekInMonth")]
    [DataRow("OffsetFromAnchor")]
    [DataRow("Algorithm")]
    public void Fixed_WhenStrategyAlreadySet_ForMonthToken_ShouldThrowInvalidOperationException(string firstStrategy)
    {
        NotableDateRuleBuilder builder = NewBuilderWithStrategy(firstStrategy);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.Fixed("March", 17);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.DayOfWeekInMonth" /> throws
    /// <see cref="InvalidOperationException" /> when any other strategy has already been selected on the
    /// builder, including a previous call to itself.
    /// </summary>
    /// <param name="firstStrategy">The strategy name applied to the builder before the DayOfWeekInMonth call.</param>
    [TestMethod]
    [DataRow("Fixed")]
    [DataRow("DayOfWeekInMonth")]
    [DataRow("OffsetFromAnchor")]
    [DataRow("Algorithm")]
    public void DayOfWeekInMonth_WhenStrategyAlreadySet_ShouldThrowInvalidOperationException(string firstStrategy)
    {
        NotableDateRuleBuilder builder = NewBuilderWithStrategy(firstStrategy);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.DayOfWeekInMonth(11, DayOfWeek.Thursday, WeekOfMonthOrdinal.Fourth);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.OffsetFromAnchor" /> throws
    /// <see cref="InvalidOperationException" /> when any other strategy has already been selected on the
    /// builder, including a previous call to itself.
    /// </summary>
    /// <param name="firstStrategy">The strategy name applied to the builder before the OffsetFromAnchor call.</param>
    [TestMethod]
    [DataRow("Fixed")]
    [DataRow("DayOfWeekInMonth")]
    [DataRow("OffsetFromAnchor")]
    [DataRow("Algorithm")]
    public void OffsetFromAnchor_WhenStrategyAlreadySet_ShouldThrowInvalidOperationException(string firstStrategy)
    {
        NotableDateRuleBuilder builder = NewBuilderWithStrategy(firstStrategy);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.OffsetFromAnchor("Easter Sunday", 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Algorithm" /> throws
    /// <see cref="InvalidOperationException" /> when any other strategy has already been selected on the
    /// builder, including a previous call to itself.
    /// </summary>
    /// <param name="firstStrategy">The strategy name applied to the builder before the Algorithm call.</param>
    [TestMethod]
    [DataRow("Fixed")]
    [DataRow("DayOfWeekInMonth")]
    [DataRow("OffsetFromAnchor")]
    [DataRow("Algorithm")]
    public void Algorithm_WhenStrategyAlreadySet_ShouldThrowInvalidOperationException(string firstStrategy)
    {
        NotableDateRuleBuilder builder = NewBuilderWithStrategy(firstStrategy);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.Algorithm(key: "easter-gregorian");
        });
    }

    /// <summary>
    /// Creates a new <see cref="NotableDateRuleBuilder" /> with the named strategy already selected.
    /// </summary>
    /// <param name="strategy">The strategy identifier — one of <c>Fixed</c>, <c>DayOfWeekInMonth</c>, <c>OffsetFromAnchor</c>, or <c>Algorithm</c>.</param>
    /// <returns>A builder whose strategy has been applied; subsequent strategy calls must throw.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="strategy" /> is not a recognised strategy identifier.</exception>
    private static NotableDateRuleBuilder NewBuilderWithStrategy(string strategy) =>
        strategy switch
        {
            "Fixed" => new NotableDateRuleBuilder().Fixed(1, 1),
            "DayOfWeekInMonth" => new NotableDateRuleBuilder().DayOfWeekInMonth(1, DayOfWeek.Monday, WeekOfMonthOrdinal.First),
            "OffsetFromAnchor" => new NotableDateRuleBuilder().OffsetFromAnchor("anchor", 1),
            "Algorithm" => new NotableDateRuleBuilder().Algorithm(key: "algo"),
            _ => throw new ArgumentException($"Unknown strategy identifier: {strategy}", nameof(strategy)),
        };

    // ============================================================================
    // Clone — deep-copy semantics for template-factory authoring.
    // ============================================================================

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Clone" /> produces a new instance rather than returning
    /// the original reference.
    /// </summary>
    [TestMethod]
    public void Clone_WhenInvoked_ShouldReturnIndependentInstance()
    {
        NotableDateRuleBuilder original = new NotableDateRuleBuilder()
            .Category(NotableDateCategory.Holiday)
            .Fixed(12, 25);

        NotableDateRuleBuilder copy = original.Clone();

        Assert.AreNotSame(original, copy);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Clone" /> produces a rule whose built representation
    /// equals the source's built representation on every authored field.
    /// </summary>
    [TestMethod]
    public void Clone_WhenSourceFullyConfigured_ShouldProduceIdenticalBuiltRule()
    {
        NotableDateRuleBuilder original = new NotableDateRuleBuilder()
            .RuleName("Christmas Fixed")
            .Category(NotableDateCategory.Holiday)
            .Territory("AU")
            .NonWorking()
            .FirstYear(1900)
            .LastYear(2099)
            .Duration(1)
            .Priority(10)
            .Comment("Test")
            .AddTag("Federal")
            .AddTag("Public")
            .Fixed(12, 25)
            .AddAdjustment("weekend-roll", adj => adj
                .When(AdjustmentTrigger.IfWeekend)
                .Action(AdjustmentAction.MoveToNextWeekday));

        NotableDateRule originalRule = original.Build("Christmas Day");
        NotableDateRule cloneRule = original.Clone().Build("Christmas Day");

        Assert.AreEqual(originalRule.Name, cloneRule.Name);
        Assert.AreEqual(originalRule.RuleName, cloneRule.RuleName);
        Assert.AreEqual(originalRule.Strategy, cloneRule.Strategy);
        Assert.AreEqual(originalRule.Category, cloneRule.Category);
        Assert.AreEqual(originalRule.TerritoryCode, cloneRule.TerritoryCode);
        Assert.AreEqual(originalRule.IsNonWorkingDay, cloneRule.IsNonWorkingDay);
        Assert.AreEqual(originalRule.FirstYear, cloneRule.FirstYear);
        Assert.AreEqual(originalRule.LastYear, cloneRule.LastYear);
        Assert.AreEqual(originalRule.DurationDays, cloneRule.DurationDays);
        Assert.AreEqual(originalRule.Priority, cloneRule.Priority);
        Assert.AreEqual(originalRule.Month, cloneRule.Month);
        Assert.AreEqual(originalRule.Day, cloneRule.Day);
        CollectionAssert.AreEquivalent(originalRule.Tags.ToList(), cloneRule.Tags.ToList());
        Assert.HasCount(originalRule.Adjustments.Length, cloneRule.Adjustments);
        Assert.AreEqual(originalRule.Adjustments[0].Key, cloneRule.Adjustments[0].Key);
    }

    /// <summary>
    /// Verifies that mutating the cloned builder's tag list after <see cref="NotableDateRuleBuilder.Clone" />
    /// has no effect on the source's tag list.
    /// </summary>
    [TestMethod]
    public void Clone_WhenCloneAddsTag_ShouldNotMutateSourceTags()
    {
        NotableDateRuleBuilder original = new NotableDateRuleBuilder()
            .Category(NotableDateCategory.Holiday)
            .AddTag("Federal")
            .Fixed(12, 25);

        NotableDateRuleBuilder copy = original.Clone().AddTag("Christian");

        NotableDateRule originalRule = original.Build("Test");
        NotableDateRule copyRule = copy.Build("Test");

        Assert.HasCount(1, originalRule.Tags);
        Assert.Contains("Federal", originalRule.Tags);
        Assert.HasCount(2, copyRule.Tags);
        Assert.Contains("Christian", copyRule.Tags);
    }

    /// <summary>
    /// Verifies that mutating the cloned builder's adjustment list after <see cref="NotableDateRuleBuilder.Clone" />
    /// has no effect on the source's adjustment list.
    /// </summary>
    [TestMethod]
    public void Clone_WhenCloneAddsAdjustment_ShouldNotMutateSourceAdjustments()
    {
        NotableDateRuleBuilder original = new NotableDateRuleBuilder()
            .Category(NotableDateCategory.Holiday)
            .Fixed(12, 25)
            .AddAdjustment("weekend-roll", adj => adj
                .When(AdjustmentTrigger.IfWeekend)
                .Action(AdjustmentAction.MoveToNextWeekday));

        NotableDateRuleBuilder copy = original.Clone()
            .AddAdjustment("leap-skip", adj => adj
                .When(AdjustmentTrigger.IfLeapYear)
                .Action(AdjustmentAction.AddDays)
                .OffsetDays(1));

        NotableDateRule originalRule = original.Build("Test");
        NotableDateRule copyRule = copy.Build("Test");

        Assert.HasCount(1, originalRule.Adjustments);
        Assert.AreEqual("weekend-roll", originalRule.Adjustments[0].Key);
        Assert.HasCount(2, copyRule.Adjustments);
    }

    // ============================================================================
    // Removal — RemoveTag / ClearTags / RemoveAdjustment / ClearAdjustments / ClearStrategy.
    // ============================================================================

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.RemoveTag" /> removes a previously-added tag.
    /// </summary>
    [TestMethod]
    public void RemoveTag_WhenTagPresent_ShouldRemoveFromBuiltRule()
    {
        NotableDateRule rule = new NotableDateRuleBuilder()
            .Category(NotableDateCategory.Holiday)
            .AddTag("Federal")
            .AddTag("Public")
            .Fixed(1, 1)
            .RemoveTag("Federal")
            .Build("Test");

        Assert.HasCount(1, rule.Tags);
        Assert.Contains("Public", rule.Tags);
        Assert.DoesNotContain("Federal", rule.Tags);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.RemoveTag" /> matches case-insensitively.
    /// </summary>
    [TestMethod]
    public void RemoveTag_WhenTagDifferentCase_ShouldStillRemove()
    {
        NotableDateRule rule = new NotableDateRuleBuilder()
            .Category(NotableDateCategory.Holiday)
            .AddTag("Federal")
            .Fixed(1, 1)
            .RemoveTag("FEDERAL")
            .Build("Test");

        Assert.IsEmpty(rule.Tags);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.RemoveTag" /> is a no-op when the tag isn't present.
    /// </summary>
    [TestMethod]
    public void RemoveTag_WhenTagAbsent_ShouldBeNoOp()
    {
        NotableDateRule rule = new NotableDateRuleBuilder()
            .Category(NotableDateCategory.Holiday)
            .AddTag("Federal")
            .Fixed(1, 1)
            .RemoveTag("Christian")
            .Build("Test");

        Assert.HasCount(1, rule.Tags);
        Assert.Contains("Federal", rule.Tags);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.RemoveTag" /> throws when the supplied value is whitespace.
    /// </summary>
    [TestMethod]
    public void RemoveTag_WhenTagWhitespace_ShouldThrowArgumentException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.RemoveTag("   ");
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.ClearTags" /> removes every previously-added tag.
    /// </summary>
    [TestMethod]
    public void ClearTags_WhenTagsPresent_ShouldYieldEmptyTagSet()
    {
        NotableDateRule rule = new NotableDateRuleBuilder()
            .Category(NotableDateCategory.Holiday)
            .AddTag("Federal")
            .AddTag("Public")
            .AddTag("Christian")
            .Fixed(1, 1)
            .ClearTags()
            .Build("Test");

        Assert.IsEmpty(rule.Tags);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.RemoveAdjustment" /> removes the adjustment whose key matches.
    /// </summary>
    [TestMethod]
    public void RemoveAdjustment_WhenKeyPresent_ShouldRemoveFromBuiltRule()
    {
        NotableDateRule rule = new NotableDateRuleBuilder()
            .Category(NotableDateCategory.Holiday)
            .Fixed(1, 1)
            .AddAdjustment("weekend-roll", adj => adj.When(AdjustmentTrigger.IfWeekend).Action(AdjustmentAction.MoveToNextWeekday))
            .AddAdjustment("leap-skip", adj => adj.When(AdjustmentTrigger.IfLeapYear).Action(AdjustmentAction.AddDays).OffsetDays(1))
            .RemoveAdjustment("weekend-roll")
            .Build("Test");

        Assert.HasCount(1, rule.Adjustments);
        Assert.AreEqual("leap-skip", rule.Adjustments[0].Key);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.RemoveAdjustment" /> matches case-insensitively.
    /// </summary>
    [TestMethod]
    public void RemoveAdjustment_WhenKeyDifferentCase_ShouldStillRemove()
    {
        NotableDateRule rule = new NotableDateRuleBuilder()
            .Category(NotableDateCategory.Holiday)
            .Fixed(1, 1)
            .AddAdjustment("Weekend-Roll", adj => adj.When(AdjustmentTrigger.IfWeekend).Action(AdjustmentAction.MoveToNextWeekday))
            .RemoveAdjustment("weekend-roll")
            .Build("Test");

        Assert.IsEmpty(rule.Adjustments);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.RemoveAdjustment" /> is a no-op when the key isn't present.
    /// </summary>
    [TestMethod]
    public void RemoveAdjustment_WhenKeyAbsent_ShouldBeNoOp()
    {
        NotableDateRule rule = new NotableDateRuleBuilder()
            .Category(NotableDateCategory.Holiday)
            .Fixed(1, 1)
            .AddAdjustment("weekend-roll", adj => adj.When(AdjustmentTrigger.IfWeekend).Action(AdjustmentAction.MoveToNextWeekday))
            .RemoveAdjustment("never-added")
            .Build("Test");

        Assert.HasCount(1, rule.Adjustments);
        Assert.AreEqual("weekend-roll", rule.Adjustments[0].Key);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.RemoveAdjustment" /> throws when the supplied key is whitespace.
    /// </summary>
    [TestMethod]
    public void RemoveAdjustment_WhenKeyWhitespace_ShouldThrowArgumentException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.RemoveAdjustment("   ");
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.ClearAdjustments" /> removes every previously-added adjustment.
    /// </summary>
    [TestMethod]
    public void ClearAdjustments_WhenAdjustmentsPresent_ShouldYieldEmptyAdjustmentSet()
    {
        NotableDateRule rule = new NotableDateRuleBuilder()
            .Category(NotableDateCategory.Holiday)
            .Fixed(1, 1)
            .AddAdjustment("weekend-roll", adj => adj.When(AdjustmentTrigger.IfWeekend).Action(AdjustmentAction.MoveToNextWeekday))
            .AddAdjustment("leap-skip", adj => adj.When(AdjustmentTrigger.IfLeapYear).Action(AdjustmentAction.AddDays).OffsetDays(1))
            .ClearAdjustments()
            .Build("Test");

        Assert.IsEmpty(rule.Adjustments);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.ClearStrategy" /> resets the strategy discriminator so that
    /// a subsequent strategy method call succeeds rather than throwing the single-strategy invariant exception.
    /// </summary>
    /// <param name="firstStrategy">The strategy applied before <c>ClearStrategy</c>.</param>
    [TestMethod]
    [DataRow("Fixed")]
    [DataRow("DayOfWeekInMonth")]
    [DataRow("OffsetFromAnchor")]
    [DataRow("Algorithm")]
    public void ClearStrategy_WhenStrategySet_ShouldAllowSubsequentStrategyCall(string firstStrategy)
    {
        NotableDateRuleBuilder builder = NewBuilderWithStrategy(firstStrategy)
            .Category(NotableDateCategory.Holiday)
            .ClearStrategy();

        // Should not throw — the strategy was cleared.
        _ = builder.Algorithm(key: "easter-gregorian");

        NotableDateRule rule = builder.Build("Test");
        Assert.AreEqual(DateResolutionStrategy.Algorithm, rule.Strategy);
        Assert.AreEqual("easter-gregorian", rule.AlgorithmKey);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.ClearStrategy" /> wipes the strategy-specific fields, so a
    /// re-authored strategy does not pick up stale values from the previous one.
    /// </summary>
    [TestMethod]
    public void ClearStrategy_WhenStrategySet_ShouldWipeStrategySpecificFields()
    {
        NotableDateRule rule = new NotableDateRuleBuilder()
            .Category(NotableDateCategory.Holiday)
            .OffsetFromAnchor("Easter Sunday", 1)
            .ClearStrategy()
            .Fixed(12, 25)
            .Build("Test");

        Assert.AreEqual(DateResolutionStrategy.Fixed, rule.Strategy);
        Assert.AreEqual(12, rule.Month);
        Assert.AreEqual(25, rule.Day);
        Assert.IsNull(rule.AnchorRuleName);
        Assert.IsNull(rule.OffsetDays);
    }
}
