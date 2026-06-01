// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFilterTests.Validation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateFilterTests
{
    // --------------------------------------------------------------------------------------
    // ForCategory — data-driven over every defined enum value
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForCategory" /> passes the primary gate for every
    /// <see cref="NotableDateCategory" /> value when the rule category is an exact match.
    /// </summary>
    [DataRow(NotableDateCategory.Holiday)]
    [DataRow(NotableDateCategory.Observance)]
    [DataRow(NotableDateCategory.Remembrance)]
    [DataRow(NotableDateCategory.Cultural)]
    [DataRow(NotableDateCategory.Seasonal)]
    [DataRow(NotableDateCategory.Other)]
    [TestMethod]
    public void ForCategory_WhenRuleCategoryMatchesEveryEnumValue_IsRuleEligibleReturnsTrue(NotableDateCategory category)
    {
        var filter = NotableDateFilter.ForCategory(category);
        NotableDateRule rule = MakeRule(category: category);

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForCategory" /> fails the primary gate for every
    /// <see cref="NotableDateCategory" /> value when the rule carries a different category.
    /// </summary>
    [DataRow(NotableDateCategory.Holiday, NotableDateCategory.Observance)]
    [DataRow(NotableDateCategory.Observance, NotableDateCategory.Cultural)]
    [DataRow(NotableDateCategory.Remembrance, NotableDateCategory.Holiday)]
    [DataRow(NotableDateCategory.Cultural, NotableDateCategory.Seasonal)]
    [DataRow(NotableDateCategory.Seasonal, NotableDateCategory.Remembrance)]
    [DataRow(NotableDateCategory.Other, NotableDateCategory.Holiday)]
    [TestMethod]
    public void ForCategory_WhenRuleCategoryDiffersFromFilterValue_IsRuleEligibleReturnsFalse(NotableDateCategory filterCategory, NotableDateCategory ruleCategory)
    {
        var filter = NotableDateFilter.ForCategory(filterCategory);
        NotableDateRule rule = MakeRule(category: ruleCategory);

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForCategory" /> passes the secondary gate for every
    /// <see cref="NotableDateCategory" /> value when the date category is an exact match.
    /// </summary>
    [DataRow(NotableDateCategory.Holiday)]
    [DataRow(NotableDateCategory.Observance)]
    [DataRow(NotableDateCategory.Remembrance)]
    [DataRow(NotableDateCategory.Cultural)]
    [DataRow(NotableDateCategory.Seasonal)]
    [DataRow(NotableDateCategory.Other)]
    [TestMethod]
    public void ForCategory_WhenDateCategoryMatchesEveryEnumValue_IsMatchReturnsTrue(NotableDateCategory category)
    {
        var filter = NotableDateFilter.ForCategory(category);
        NotableDate date = MakeDate(category: category);

        Assert.IsTrue(filter.IsMatch(date));
    }

    // --------------------------------------------------------------------------------------
    // WithMinDuration — data-driven boundary values at both gates
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithMinDuration" /> passes or fails the primary gate
    /// according to whether the rule's <see cref="NotableDateRule.DurationDays" /> satisfies the minimum.
    /// </summary>
    [DataRow(1, 1, true)]
    [DataRow(1, 5, true)]
    [DataRow(2, 1, false)]
    [DataRow(3, 3, true)]
    [DataRow(3, 2, false)]
    [DataRow(7, 10, true)]
    [DataRow(7, 6, false)]
    [TestMethod]
    public void WithMinDuration_RuleGate_WhenDurationVariesRelativeToMinimum_ShouldMatchExpected(int minimumDays, int durationDays, bool expectedMatch)
    {
        var filter = NotableDateFilter.WithMinDuration(minimumDays);
        NotableDateRule rule = MakeRule(durationDays: durationDays);

        Assert.AreEqual(expectedMatch, filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithMinDuration" /> passes or fails the secondary gate
    /// according to whether the date's <see cref="NotableDate.DurationDays" /> satisfies the minimum.
    /// </summary>
    [DataRow(1, 1, true)]
    [DataRow(1, 5, true)]
    [DataRow(2, 1, false)]
    [DataRow(3, 3, true)]
    [DataRow(3, 2, false)]
    [DataRow(7, 10, true)]
    [DataRow(7, 6, false)]
    [TestMethod]
    public void WithMinDuration_DateGate_WhenDurationVariesRelativeToMinimum_ShouldMatchExpected(int minimumDays, int durationDays, bool expectedMatch)
    {
        var filter = NotableDateFilter.WithMinDuration(minimumDays);
        NotableDate date = MakeDate(durationDays: durationDays);

        Assert.AreEqual(expectedMatch, filter.IsMatch(date));
    }

    // --------------------------------------------------------------------------------------
    // Argument validation — gaps not covered in the primary test file
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> throws <see cref="ArgumentException" /> when
    /// <paramref name="tag" /> consists solely of whitespace.
    /// </summary>
    [TestMethod]
    public void WithTag_WhenTagIsWhitespace_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateFilter.WithTag("   ");
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyTag" /> throws <see cref="ArgumentNullException" /> when
    /// the tags array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithAnyTag_WhenTagsIsNull_ShouldThrowExactly()
    {
        string[] tags = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithAnyTag(tags);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyTag" /> throws <see cref="ArgumentException" /> when
    /// the tags array contains no elements.
    /// </summary>
    [TestMethod]
    public void WithAnyTag_WhenTagsIsEmpty_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateFilter.WithAnyTag();
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> throws <see cref="ArgumentNullException" /> when
    /// the tags array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithAllTags_WhenTagsIsNull_ShouldThrowExactly()
    {
        string[] tags = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithAllTags(tags);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> throws <see cref="ArgumentException" /> when
    /// the tags array contains no elements.
    /// </summary>
    [TestMethod]
    public void WithAllTags_WhenTagsIsEmpty_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateFilter.WithAllTags();
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithName" /> throws <see cref="ArgumentException" /> when
    /// <paramref name="name" /> consists solely of whitespace.
    /// </summary>
    [TestMethod]
    public void WithName_WhenNameIsWhitespace_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateFilter.WithName("   ");
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyName" /> throws <see cref="ArgumentNullException" /> when
    /// the names array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithAnyName_WhenNamesIsNull_ShouldThrowExactly()
    {
        string[] names = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithAnyName(names);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyName" /> throws <see cref="ArgumentException" /> when
    /// the names array contains no elements.
    /// </summary>
    [TestMethod]
    public void WithAnyName_WhenNamesIsEmpty_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateFilter.WithAnyName();
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AllOf" /> throws <see cref="ArgumentNullException" /> when
    /// the filters array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AllOf_WhenFiltersIsNull_ShouldThrowExactly()
    {
        NotableDateFilter[] filters = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.AllOf(filters);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AnyOf" /> throws <see cref="ArgumentNullException" /> when
    /// the filters array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AnyOf_WhenFiltersIsNull_ShouldThrowExactly()
    {
        NotableDateFilter[] filters = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.AnyOf(filters);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithMinDuration" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when <paramref name="minimumDays" /> is negative.
    /// </summary>
    [TestMethod]
    public void WithMinDuration_WhenMinimumDaysIsNegative_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = NotableDateFilter.WithMinDuration(-1);
        });
    }

    // --------------------------------------------------------------------------------------
    // Secondary gate — gaps not covered in the primary test file
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyTag" /> passes the secondary gate when the date's
    /// tags contain at least one of the accepted values (case-insensitive).
    /// </summary>
    [TestMethod]
    public void WithAnyTag_WhenDateHasAtLeastOneMatchingTag_IsMatchReturnsTrue()
    {
        var filter = NotableDateFilter.WithAnyTag("Public", "Federal");
        NotableDate date = MakeDate(tags: ["Regional", "FEDERAL"]);

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyTag" /> fails the secondary gate when the date's
    /// tags contain none of the accepted values.
    /// </summary>
    [TestMethod]
    public void WithAnyTag_WhenDateHasNoMatchingTag_IsMatchReturnsFalse()
    {
        var filter = NotableDateFilter.WithAnyTag("Public", "Federal");
        NotableDate date = MakeDate(tags: ["Christian"]);

        Assert.IsFalse(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> passes the secondary gate when the date's
    /// tags contain every required value (case-insensitive).
    /// </summary>
    [TestMethod]
    public void WithAllTags_WhenDateHasAllRequiredTags_IsMatchReturnsTrue()
    {
        var filter = NotableDateFilter.WithAllTags("Public", "Christian");
        NotableDate date = MakeDate(tags: ["Christian", "Public", "Federal"]);

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> fails the secondary gate when the date's
    /// tags are missing at least one required value.
    /// </summary>
    [TestMethod]
    public void WithAllTags_WhenDateIsMissingARequiredTag_IsMatchReturnsFalse()
    {
        var filter = NotableDateFilter.WithAllTags("Public", "Federal");
        NotableDate date = MakeDate(tags: ["Public"]);

        Assert.IsFalse(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithName" /> passes the secondary gate when the date name
    /// matches case-insensitively.
    /// </summary>
    [TestMethod]
    public void WithName_WhenDateNameMatchesCaseInsensitively_IsMatchReturnsTrue()
    {
        var filter = NotableDateFilter.WithName("christmas day");
        NotableDate date = MakeDate(name: "Christmas Day");

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithName" /> fails the secondary gate when the date name
    /// does not match.
    /// </summary>
    [TestMethod]
    public void WithName_WhenDateNameDoesNotMatch_IsMatchReturnsFalse()
    {
        var filter = NotableDateFilter.WithName("Christmas Day");
        NotableDate date = MakeDate(name: "Easter Sunday");

        Assert.IsFalse(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyName" /> passes the secondary gate when the date name
    /// is one of the accepted values.
    /// </summary>
    [TestMethod]
    public void WithAnyName_WhenDateNameIsOneOfAccepted_IsMatchReturnsTrue()
    {
        var filter = NotableDateFilter.WithAnyName("Christmas Day", "Easter Sunday");
        NotableDate date = MakeDate(name: "Easter Sunday");

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyName" /> fails the secondary gate when the date name
    /// is not one of the accepted values.
    /// </summary>
    [TestMethod]
    public void WithAnyName_WhenDateNameIsNotAccepted_IsMatchReturnsFalse()
    {
        var filter = NotableDateFilter.WithAnyName("Christmas Day", "Easter Sunday");
        NotableDate date = MakeDate(name: "Anzac Day");

        Assert.IsFalse(filter.IsMatch(date));
    }

    // --------------------------------------------------------------------------------------
    // IsNonWorkingDay — primary gate with adjustment-mechanism variants
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.IsNonWorkingDay" /> passes the primary gate when the rule's
    /// <see cref="NotableDateRule.IsNonWorkingDay" /> is <see langword="null" /> but an adjustment
    /// explicitly sets the non-working flag.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenRuleHasAdjustmentWithNonWorkingFlag_IsRuleEligibleReturnsTrue()
    {
        var filter = NotableDateFilter.IsNonWorkingDay();
        NotableDateRule rule = MakeRule(isNonWorkingDay: null) with
        {
            Adjustments = [new ObservanceAdjustment
            {
                Key = "nwd-via-adjustment",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextWeekday,
                IsNonWorkingDay = true,
            }],
        };

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.IsNonWorkingDay" /> passes the primary gate when the rule
    /// has an adjustment with a <see cref="AdjustmentAction.Custom" /> action, which may set the
    /// non-working flag at runtime and cannot be excluded statically.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenRuleHasAdjustmentWithCustomAction_IsRuleEligibleReturnsTrue()
    {
        var filter = NotableDateFilter.IsNonWorkingDay();
        NotableDateRule rule = MakeRule(isNonWorkingDay: null) with
        {
            Adjustments = [new ObservanceAdjustment
            {
                Key = "custom-action",
                Trigger = AdjustmentTrigger.Always,
                Action = AdjustmentAction.Custom,
            }],
        };

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.IsNonWorkingDay" /> passes the primary gate when the rule
    /// has an adjustment with a <see cref="AdjustmentTrigger.Custom" /> trigger, which may produce a
    /// non-working date at runtime and cannot be excluded statically.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenRuleHasAdjustmentWithCustomTrigger_IsRuleEligibleReturnsTrue()
    {
        var filter = NotableDateFilter.IsNonWorkingDay();
        NotableDateRule rule = MakeRule(isNonWorkingDay: null) with
        {
            Adjustments = [new ObservanceAdjustment
            {
                Key = "custom-trigger",
                Trigger = AdjustmentTrigger.Custom,
                Action = AdjustmentAction.MoveToNextWeekday,
            }],
        };

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.IsNonWorkingDay" /> fails the primary gate when the rule's
    /// <see cref="NotableDateRule.IsNonWorkingDay" /> is <see langword="null" /> and no adjustment provides
    /// any non-working mechanism.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenRuleIsNonWorkingNullAndHasNoAdjustments_IsRuleEligibleReturnsFalse()
    {
        var filter = NotableDateFilter.IsNonWorkingDay();
        NotableDateRule rule = MakeRule(isNonWorkingDay: null);

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.IsNonWorkingDay" /> fails the primary gate when the rule has
    /// an adjustment that sets <see cref="ObservanceAdjustment.IsNonWorkingDay" /> to <see langword="false" />,
    /// explicitly opting out rather than providing a non-working mechanism.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenRuleHasAdjustmentWithNonWorkingFalse_IsRuleEligibleReturnsFalse()
    {
        var filter = NotableDateFilter.IsNonWorkingDay();
        NotableDateRule rule = MakeRule(isNonWorkingDay: null) with
        {
            Adjustments = [new ObservanceAdjustment
            {
                Key = "nwd-false",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextWeekday,
                IsNonWorkingDay = false,
            }],
        };

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    // --------------------------------------------------------------------------------------
    // InDateRange — boundary and edge-case tests
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> passes the secondary gate when the date
    /// falls exactly on the inclusive start boundary.
    /// </summary>
    [TestMethod]
    public void InDateRange_WhenDateIsExactlyOnStartBoundary_IsMatchReturnsTrue()
    {
        var start = new DateTime(2024, 6, 1);
        var end = new DateTime(2024, 6, 30);
        var filter = NotableDateFilter.InDateRange(start, end);
        NotableDate date = MakeDate(date: start);

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> passes the secondary gate when the date
    /// falls exactly on the inclusive end boundary.
    /// </summary>
    [TestMethod]
    public void InDateRange_WhenDateIsExactlyOnEndBoundary_IsMatchReturnsTrue()
    {
        var start = new DateTime(2024, 6, 1);
        var end = new DateTime(2024, 6, 30);
        var filter = NotableDateFilter.InDateRange(start, end);
        NotableDate date = MakeDate(date: end);

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> passes the secondary gate when the start
    /// and end dates are identical and the date matches that single day.
    /// </summary>
    [TestMethod]
    public void InDateRange_WhenStartEqualsEndAndDateMatchesThatDay_IsMatchReturnsTrue()
    {
        var day = new DateTime(2024, 6, 15);
        var filter = NotableDateFilter.InDateRange(day, day);
        NotableDate date = MakeDate(date: day);

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> fails the secondary gate when the start and
    /// end dates are identical but the date falls outside that single day.
    /// </summary>
    [TestMethod]
    public void InDateRange_WhenStartEqualsEndAndDateIsOutside_IsMatchReturnsFalse()
    {
        var day = new DateTime(2024, 6, 15);
        var filter = NotableDateFilter.InDateRange(day, day);
        NotableDate date = MakeDate(date: new DateTime(2024, 6, 16));

        Assert.IsFalse(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> strips time components from the supplied
    /// bounds so that a date at midnight on the start day still falls within a range whose lower bound
    /// carries a non-zero time.
    /// </summary>
    [TestMethod]
    public void InDateRange_WhenBoundsHaveTimeComponents_ShouldCompareDateOnly()
    {
        // Supplying bounds with time parts; after stripping these become 2024-06-01 and 2024-06-30.
        var filter = NotableDateFilter.InDateRange(
            new DateTime(2024, 6, 1, 23, 59, 59),
            new DateTime(2024, 6, 30, 0, 0, 1));
        NotableDate date = MakeDate(date: new DateTime(2024, 6, 1));

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> fails the secondary gate when a multi-day
    /// span ends on the day immediately before the range starts, leaving no overlap.
    /// </summary>
    [TestMethod]
    public void InDateRange_WhenMultiDaySpanEndsJustBeforeRangeStart_IsMatchReturnsFalse()
    {
        // Four-day span 28–31 May; range begins 1 June — no day in common.
        var filter = NotableDateFilter.InDateRange(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        NotableDate date = MakeDate(date: new DateTime(2024, 5, 28), durationDays: 4);

        Assert.IsFalse(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> fails the secondary gate when a multi-day
    /// span begins on the day immediately after the range ends, leaving no overlap.
    /// </summary>
    [TestMethod]
    public void InDateRange_WhenMultiDaySpanStartsJustAfterRangeEnd_IsMatchReturnsFalse()
    {
        // Range ends 30 June; three-day span starts 1 July — no day in common.
        var filter = NotableDateFilter.InDateRange(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        NotableDate date = MakeDate(date: new DateTime(2024, 7, 1), durationDays: 3);

        Assert.IsFalse(filter.IsMatch(date));
    }

    // --------------------------------------------------------------------------------------
    // AllOf / AnyOf — primary gate and null argument validation
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AllOf" /> passes the primary gate only when every
    /// supplied filter's primary gate passes.
    /// </summary>
    [TestMethod]
    public void AllOf_WhenAllPrimaryGatesPass_IsRuleEligibleReturnsTrue()
    {
        var filter = NotableDateFilter.AllOf(
            NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            NotableDateFilter.WithTag("Public"),
            NotableDateFilter.IsNonWorkingDay());
        NotableDateRule rule = MakeRule(
            category: NotableDateCategory.Holiday,
            isNonWorkingDay: true,
            tags: ["Public"]);

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AllOf" /> fails the primary gate when at least one
    /// supplied filter's primary gate fails.
    /// </summary>
    [TestMethod]
    public void AllOf_WhenOnePrimaryGateFails_IsRuleEligibleReturnsFalse()
    {
        var filter = NotableDateFilter.AllOf(
            NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            NotableDateFilter.WithTag("Public"));
        NotableDateRule rule = MakeRule(category: NotableDateCategory.Observance, tags: ["Public"]);

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AnyOf" /> passes the primary gate when at least one
    /// supplied filter's primary gate passes.
    /// </summary>
    [TestMethod]
    public void AnyOf_WhenOnePrimaryGatePasses_IsRuleEligibleReturnsTrue()
    {
        var filter = NotableDateFilter.AnyOf(
            NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            NotableDateFilter.ForCategory(NotableDateCategory.Observance));
        NotableDateRule rule = MakeRule(category: NotableDateCategory.Observance);

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AnyOf" /> fails the primary gate when every supplied
    /// filter's primary gate fails.
    /// </summary>
    [TestMethod]
    public void AnyOf_WhenAllPrimaryGatesFail_IsRuleEligibleReturnsFalse()
    {
        var filter = NotableDateFilter.AnyOf(
            NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            NotableDateFilter.ForCategory(NotableDateCategory.Observance));
        NotableDateRule rule = MakeRule(category: NotableDateCategory.Cultural);

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AnyOf" /> passes the secondary gate when every supplied
    /// filter matches — confirming the OR is not exclusive.
    /// </summary>
    [TestMethod]
    public void AnyOf_WhenAllFiltersMatch_IsMatchReturnsTrue()
    {
        var filter = NotableDateFilter.AnyOf(
            NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            NotableDateFilter.IsNonWorkingDay());
        NotableDate date = MakeDate(category: NotableDateCategory.Holiday, isNonWorkingDay: true);

        Assert.IsTrue(filter.IsMatch(date));
    }

    // --------------------------------------------------------------------------------------
    // ForAnyCategory — secondary gate
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> passes the secondary gate when the date
    /// category is one of the accepted values.
    /// </summary>
    [TestMethod]
    public void ForAnyCategory_WhenDateCategoryIsOneOfAccepted_IsMatchReturnsTrue()
    {
        var filter = NotableDateFilter.ForAnyCategory(NotableDateCategory.Holiday, NotableDateCategory.Observance);
        NotableDate date = MakeDate(category: NotableDateCategory.Observance);

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> fails the secondary gate when the date
    /// category is not one of the accepted values.
    /// </summary>
    [TestMethod]
    public void ForAnyCategory_WhenDateCategoryIsNotAccepted_IsMatchReturnsFalse()
    {
        var filter = NotableDateFilter.ForAnyCategory(NotableDateCategory.Holiday, NotableDateCategory.Observance);
        NotableDate date = MakeDate(category: NotableDateCategory.Cultural);

        Assert.IsFalse(filter.IsMatch(date));
    }

    // --------------------------------------------------------------------------------------
    // WasAdjusted — primary gate always passes
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WasAdjusted" /> always passes the primary gate regardless
    /// of rule properties, since adjustment outcome cannot be determined before date resolution.
    /// </summary>
    [TestMethod]
    public void WasAdjusted_ForRuleWithNonWorkingFlag_IsRuleEligibleAlwaysReturnsTrue()
    {
        var filter = NotableDateFilter.WasAdjusted();
        NotableDateRule rule = MakeRule(isNonWorkingDay: true);

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WasAdjusted" /> always passes the primary gate for a rule
    /// that cannot produce an adjusted date, confirming the gate is unconditional.
    /// </summary>
    [TestMethod]
    public void WasAdjusted_ForRuleWithNoAdjustments_IsRuleEligibleAlwaysReturnsTrue()
    {
        var filter = NotableDateFilter.WasAdjusted();
        NotableDateRule rule = MakeRule(isNonWorkingDay: false);

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }
}
