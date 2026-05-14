// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFilterTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the behaviour of <see cref="NotableDateFilter" /> factory methods, argument validation, primary-gate rule eligibility,
/// secondary-gate date matching, and And/Or composition.
/// </summary>
[TestClass]
public sealed partial class NotableDateFilterTests
{
    // --------------------------------------------------------------------------------------
    // Helpers
    // --------------------------------------------------------------------------------------

    private static NotableDateRule MakeRule(
        string name = "Test",
        NotableDateCategory category = NotableDateCategory.Holiday,
        bool? isNonWorkingDay = null,
        ImmutableHashSet<string>? tags = null,
        int durationDays = 1) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = category,
            Month = 1,
            Day = 1,
            IsNonWorkingDay = isNonWorkingDay,
            Tags = tags ?? ImmutableHashSet<string>.Empty,
            DurationDays = durationDays,
        };

    private static NotableDate MakeDate(
        string name = "Test",
        NotableDateCategory category = NotableDateCategory.Holiday,
        bool isNonWorkingDay = false,
        ImmutableHashSet<string>? tags = null,
        DateTime? date = null,
        int durationDays = 1,
        AdjustmentReason? adjustmentReason = null) =>
        new()
        {
            Name = name,
            Date = date ?? new DateTime(2024, 1, 1),
            Category = category,
            IsNonWorkingDay = isNonWorkingDay,
            Tags = tags ?? ImmutableHashSet<string>.Empty,
            DurationDays = durationDays,
            AdjustmentReason = adjustmentReason,
        };

    // --------------------------------------------------------------------------------------
    // ForCategory
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForCategory" /> passes the primary gate when the rule category matches.
    /// </summary>
    [TestMethod]
    public void ForCategory_WhenRuleCategoryMatches_IsRuleEligibleReturnsTrue()
    {
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
        NotableDateRule rule = MakeRule(category: NotableDateCategory.Holiday);

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForCategory" /> fails the primary gate when the rule category does not match.
    /// </summary>
    [TestMethod]
    public void ForCategory_WhenRuleCategoryDoesNotMatch_IsRuleEligibleReturnsFalse()
    {
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
        NotableDateRule rule = MakeRule(category: NotableDateCategory.Observance);

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForCategory" /> passes the secondary gate when the date category matches.
    /// </summary>
    [TestMethod]
    public void ForCategory_WhenDateCategoryMatches_IsMatchReturnsTrue()
    {
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Cultural);
        NotableDate date = MakeDate(category: NotableDateCategory.Cultural);

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForCategory" /> fails the secondary gate when the date category does not match.
    /// </summary>
    [TestMethod]
    public void ForCategory_WhenDateCategoryDoesNotMatch_IsMatchReturnsFalse()
    {
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Cultural);
        NotableDate date = MakeDate(category: NotableDateCategory.Holiday);

        Assert.IsFalse(filter.IsMatch(date));
    }

    // --------------------------------------------------------------------------------------
    // ForAnyCategory
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> passes the primary gate when the rule category is one of the
    /// accepted values.
    /// </summary>
    [TestMethod]
    public void ForAnyCategory_WhenRuleCategoryIsOneOfAccepted_IsRuleEligibleReturnsTrue()
    {
        var filter = NotableDateFilter.ForAnyCategory(NotableDateCategory.Holiday, NotableDateCategory.Observance);
        NotableDateRule rule = MakeRule(category: NotableDateCategory.Observance);

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> fails the primary gate when the rule category is not one of
    /// the accepted values.
    /// </summary>
    [TestMethod]
    public void ForAnyCategory_WhenRuleCategoryIsNotAccepted_IsRuleEligibleReturnsFalse()
    {
        var filter = NotableDateFilter.ForAnyCategory(NotableDateCategory.Holiday, NotableDateCategory.Observance);
        NotableDateRule rule = MakeRule(category: NotableDateCategory.Cultural);

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> throws <see cref="ArgumentNullException" /> when the
    /// categories array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ForAnyCategory_WhenCategoriesIsNull_ShouldThrowArgumentNullException()
    {
        NotableDateCategory[] categories = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.ForAnyCategory(categories);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> throws <see cref="ArgumentException" /> when the categories
    /// array is empty.
    /// </summary>
    [TestMethod]
    public void ForAnyCategory_WhenCategoriesIsEmpty_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateFilter.ForAnyCategory();
        });
    }

    // --------------------------------------------------------------------------------------
    // WithTag
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> passes the primary gate when the rule contains the tag
    /// (case-insensitive).
    /// </summary>
    [TestMethod]
    public void WithTag_WhenRuleHasMatchingTag_IsRuleEligibleReturnsTrue()
    {
        var filter = NotableDateFilter.WithTag("Public");
        NotableDateRule rule = MakeRule(tags: ImmutableHashSet.Create("public", "Federal"));

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> fails the primary gate when the rule does not contain the tag.
    /// </summary>
    [TestMethod]
    public void WithTag_WhenRuleDoesNotHaveTag_IsRuleEligibleReturnsFalse()
    {
        var filter = NotableDateFilter.WithTag("Public");
        NotableDateRule rule = MakeRule(tags: ImmutableHashSet.Create("Christian"));

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> passes the secondary gate when the date contains the tag
    /// (case-insensitive).
    /// </summary>
    [TestMethod]
    public void WithTag_WhenDateHasMatchingTag_IsMatchReturnsTrue()
    {
        var filter = NotableDateFilter.WithTag("Federal");
        NotableDate date = MakeDate(tags: ImmutableHashSet.Create("FEDERAL"));

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> throws <see cref="ArgumentNullException" /> when <paramref name="tag" />
    /// is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithTag_WhenTagIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithTag(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> throws <see cref="ArgumentException" /> when <paramref name="tag" />
    /// is empty.
    /// </summary>
    [TestMethod]
    public void WithTag_WhenTagIsEmpty_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateFilter.WithTag(string.Empty);
        });
    }

    // --------------------------------------------------------------------------------------
    // WithAnyTag
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyTag" /> passes the primary gate when the rule contains at least one of the
    /// accepted tags.
    /// </summary>
    [TestMethod]
    public void WithAnyTag_WhenRuleHasAtLeastOneMatchingTag_IsRuleEligibleReturnsTrue()
    {
        var filter = NotableDateFilter.WithAnyTag("Public", "Federal");
        NotableDateRule rule = MakeRule(tags: ImmutableHashSet.Create("Christian", "Public"));

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyTag" /> fails the primary gate when the rule contains none of the accepted
    /// tags.
    /// </summary>
    [TestMethod]
    public void WithAnyTag_WhenRuleHasNoMatchingTag_IsRuleEligibleReturnsFalse()
    {
        var filter = NotableDateFilter.WithAnyTag("Public", "Federal");
        NotableDateRule rule = MakeRule(tags: ImmutableHashSet.Create("Christian"));

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    // --------------------------------------------------------------------------------------
    // WithAllTags
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> passes the primary gate only when the rule contains every required
    /// tag.
    /// </summary>
    [TestMethod]
    public void WithAllTags_WhenRuleHasAllRequiredTags_IsRuleEligibleReturnsTrue()
    {
        var filter = NotableDateFilter.WithAllTags("Public", "Christian");
        NotableDateRule rule = MakeRule(tags: ImmutableHashSet.Create("Christian", "Public", "Federal"));

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> fails the primary gate when at least one required tag is absent.
    /// </summary>
    [TestMethod]
    public void WithAllTags_WhenRuleIsMissingARequiredTag_IsRuleEligibleReturnsFalse()
    {
        var filter = NotableDateFilter.WithAllTags("Public", "Federal");
        NotableDateRule rule = MakeRule(tags: ImmutableHashSet.Create("Public"));

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    // --------------------------------------------------------------------------------------
    // WithName
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithName" /> passes the primary gate when the rule name matches
    /// case-insensitively.
    /// </summary>
    [TestMethod]
    public void WithName_WhenRuleNameMatchesCaseInsensitively_IsRuleEligibleReturnsTrue()
    {
        var filter = NotableDateFilter.WithName("christmas day");
        NotableDateRule rule = MakeRule(name: "Christmas Day");

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithName" /> fails the primary gate when the rule name does not match.
    /// </summary>
    [TestMethod]
    public void WithName_WhenRuleNameDoesNotMatch_IsRuleEligibleReturnsFalse()
    {
        var filter = NotableDateFilter.WithName("Christmas Day");
        NotableDateRule rule = MakeRule(name: "Easter Sunday");

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    // --------------------------------------------------------------------------------------
    // WithAnyName
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyName" /> passes the primary gate when the rule name is one of the accepted
    /// names.
    /// </summary>
    [TestMethod]
    public void WithAnyName_WhenRuleNameIsOneOfAccepted_IsRuleEligibleReturnsTrue()
    {
        var filter = NotableDateFilter.WithAnyName("Christmas Day", "Easter Sunday");
        NotableDateRule rule = MakeRule(name: "Easter Sunday");

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyName" /> fails the primary gate when the rule name is not one of the
    /// accepted names.
    /// </summary>
    [TestMethod]
    public void WithAnyName_WhenRuleNameIsNotAccepted_IsRuleEligibleReturnsFalse()
    {
        var filter = NotableDateFilter.WithAnyName("Christmas Day", "Easter Sunday");
        NotableDateRule rule = MakeRule(name: "Anzac Day");

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    // --------------------------------------------------------------------------------------
    // IsNonWorkingDay
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.IsNonWorkingDay" /> passes the primary gate when the rule is explicitly flagged as
    /// a non-working day.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenRuleIsExplicitlyNonWorking_IsRuleEligibleReturnsTrue()
    {
        var filter = NotableDateFilter.IsNonWorkingDay();
        NotableDateRule rule = MakeRule(isNonWorkingDay: true);

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.IsNonWorkingDay" /> fails the primary gate when the rule has no non-working
    /// mechanism.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenRuleHasNoNonWorkingMechanism_IsRuleEligibleReturnsFalse()
    {
        var filter = NotableDateFilter.IsNonWorkingDay();
        NotableDateRule rule = MakeRule(isNonWorkingDay: false);

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.IsNonWorkingDay" /> passes the secondary gate when the date is a non-working day.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenDateIsNonWorkingDay_IsMatchReturnsTrue()
    {
        var filter = NotableDateFilter.IsNonWorkingDay();
        NotableDate date = MakeDate(isNonWorkingDay: true);

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.IsNonWorkingDay" /> fails the secondary gate when the date is not a non-working
    /// day.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenDateIsWorkingDay_IsMatchReturnsFalse()
    {
        var filter = NotableDateFilter.IsNonWorkingDay();
        NotableDate date = MakeDate(isNonWorkingDay: false);

        Assert.IsFalse(filter.IsMatch(date));
    }

    // --------------------------------------------------------------------------------------
    // InDateRange
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> always passes the primary gate because date ranges cannot be
    /// determined from rule metadata alone.
    /// </summary>
    [TestMethod]
    public void InDateRange_IsRuleEligibleAlwaysReturnsTrue()
    {
        var filter = NotableDateFilter.InDateRange(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        NotableDateRule rule = MakeRule();

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> passes the secondary gate when the date falls within the range.
    /// </summary>
    [TestMethod]
    public void InDateRange_WhenDateIsWithinRange_IsMatchReturnsTrue()
    {
        var filter = NotableDateFilter.InDateRange(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        NotableDate date = MakeDate(date: new DateTime(2024, 6, 15));

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> fails the secondary gate when the date falls outside the range.
    /// </summary>
    [TestMethod]
    public void InDateRange_WhenDateIsOutsideRange_IsMatchReturnsFalse()
    {
        var filter = NotableDateFilter.InDateRange(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        NotableDate date = MakeDate(date: new DateTime(2024, 7, 1));

        Assert.IsFalse(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> passes the secondary gate when a multi-day span's end date
    /// reaches into the range even though the anchor falls before it.
    /// </summary>
    [TestMethod]
    public void InDateRange_WhenMultiDaySpanOverlapsRange_IsMatchReturnsTrue()
    {
        var filter = NotableDateFilter.InDateRange(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        NotableDate date = MakeDate(date: new DateTime(2024, 5, 30), durationDays: 5);

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> throws <see cref="ArgumentException" /> when
    /// <paramref name="endDate" /> is earlier than <paramref name="startDate" />.
    /// </summary>
    [TestMethod]
    public void InDateRange_WhenEndDateIsBeforeStartDate_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = NotableDateFilter.InDateRange(new DateTime(2024, 6, 30), new DateTime(2024, 6, 1));
        });
    }

    // --------------------------------------------------------------------------------------
    // WasAdjusted
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WasAdjusted" /> always passes the primary gate because adjustment outcome is not
    /// known at the rule level.
    /// </summary>
    [TestMethod]
    public void WasAdjusted_IsRuleEligibleAlwaysReturnsTrue()
    {
        var filter = NotableDateFilter.WasAdjusted();
        NotableDateRule rule = MakeRule();

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WasAdjusted" /> passes the secondary gate when the date has been adjusted.
    /// </summary>
    [TestMethod]
    public void WasAdjusted_WhenDateWasAdjusted_IsMatchReturnsTrue()
    {
        var filter = NotableDateFilter.WasAdjusted();
        AdjustmentReason reason = new(new DateTime(2024, 1, 1), AdjustmentTrigger.IfWeekend, AdjustmentAction.MoveToNextWeekday, null);
        NotableDate date = MakeDate(adjustmentReason: reason);

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WasAdjusted" /> fails the secondary gate when the date has not been adjusted.
    /// </summary>
    [TestMethod]
    public void WasAdjusted_WhenDateWasNotAdjusted_IsMatchReturnsFalse()
    {
        var filter = NotableDateFilter.WasAdjusted();
        NotableDate date = MakeDate(adjustmentReason: null);

        Assert.IsFalse(filter.IsMatch(date));
    }

    // --------------------------------------------------------------------------------------
    // WithMinDuration
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithMinDuration" /> passes the primary gate when the rule duration meets the
    /// minimum.
    /// </summary>
    [TestMethod]
    public void WithMinDuration_WhenRuleDurationMeetsMinimum_IsRuleEligibleReturnsTrue()
    {
        var filter = NotableDateFilter.WithMinDuration(3);
        NotableDateRule rule = MakeRule(durationDays: 5);

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithMinDuration" /> fails the primary gate when the rule duration is below the
    /// minimum.
    /// </summary>
    [TestMethod]
    public void WithMinDuration_WhenRuleDurationIsBelowMinimum_IsRuleEligibleReturnsFalse()
    {
        var filter = NotableDateFilter.WithMinDuration(3);
        NotableDateRule rule = MakeRule(durationDays: 2);

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithMinDuration" /> throws <see cref="ArgumentOutOfRangeException" /> when
    /// <paramref name="minimumDays" /> is less than one.
    /// </summary>
    [TestMethod]
    public void WithMinDuration_WhenMinimumDaysIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = NotableDateFilter.WithMinDuration(0);
        });
    }

    // --------------------------------------------------------------------------------------
    // And
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.And" /> passes the primary gate only when both component gates pass.
    /// </summary>
    [TestMethod]
    public void And_WhenBothPrimaryGatesPass_IsRuleEligibleReturnsTrue()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday)
            .And(NotableDateFilter.WithTag("Public"));
        NotableDateRule rule = MakeRule(category: NotableDateCategory.Holiday, tags: ImmutableHashSet.Create("Public"));

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.And" /> fails the primary gate when the first component gate fails.
    /// </summary>
    [TestMethod]
    public void And_WhenFirstPrimaryGateFails_IsRuleEligibleReturnsFalse()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday)
            .And(NotableDateFilter.WithTag("Public"));
        NotableDateRule rule = MakeRule(category: NotableDateCategory.Observance, tags: ImmutableHashSet.Create("Public"));

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.And" /> fails the primary gate when the second component gate fails.
    /// </summary>
    [TestMethod]
    public void And_WhenSecondPrimaryGateFails_IsRuleEligibleReturnsFalse()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday)
            .And(NotableDateFilter.WithTag("Public"));
        NotableDateRule rule = MakeRule(category: NotableDateCategory.Holiday, tags: ImmutableHashSet.Create("Christian"));

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.And" /> passes the secondary gate only when both component gates match the date.
    /// </summary>
    [TestMethod]
    public void And_WhenBothSecondaryGatesPass_IsMatchReturnsTrue()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday)
            .And(NotableDateFilter.IsNonWorkingDay());
        NotableDate date = MakeDate(category: NotableDateCategory.Holiday, isNonWorkingDay: true);

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.And" /> fails the secondary gate when the second component gate does not match.
    /// </summary>
    [TestMethod]
    public void And_WhenOneSecondaryGateFails_IsMatchReturnsFalse()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday)
            .And(NotableDateFilter.IsNonWorkingDay());
        NotableDate date = MakeDate(category: NotableDateCategory.Holiday, isNonWorkingDay: false);

        Assert.IsFalse(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.And" /> throws <see cref="ArgumentNullException" /> when <paramref name="other" />
    /// is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void And_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = filter.And(null!);
        });
    }

    // --------------------------------------------------------------------------------------
    // Or
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Or" /> passes the primary gate when at least one component gate passes.
    /// </summary>
    [TestMethod]
    public void Or_WhenOnePrimaryGatePasses_IsRuleEligibleReturnsTrue()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday)
            .Or(NotableDateFilter.ForCategory(NotableDateCategory.Observance));
        NotableDateRule rule = MakeRule(category: NotableDateCategory.Observance);

        Assert.IsTrue(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Or" /> fails the primary gate when neither component gate passes.
    /// </summary>
    [TestMethod]
    public void Or_WhenNoPrimaryGatePasses_IsRuleEligibleReturnsFalse()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday)
            .Or(NotableDateFilter.ForCategory(NotableDateCategory.Observance));
        NotableDateRule rule = MakeRule(category: NotableDateCategory.Cultural);

        Assert.IsFalse(filter.IsRuleEligible(rule));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Or" /> passes the secondary gate when the second component matches the date.
    /// </summary>
    [TestMethod]
    public void Or_WhenSecondSecondaryGatePasses_IsMatchReturnsTrue()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday)
            .Or(NotableDateFilter.IsNonWorkingDay());
        NotableDate date = MakeDate(category: NotableDateCategory.Observance, isNonWorkingDay: true);

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Or" /> fails the secondary gate when neither component matches the date.
    /// </summary>
    [TestMethod]
    public void Or_WhenNoSecondaryGatePasses_IsMatchReturnsFalse()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday)
            .Or(NotableDateFilter.IsNonWorkingDay());
        NotableDate date = MakeDate(category: NotableDateCategory.Observance, isNonWorkingDay: false);

        Assert.IsFalse(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Or" /> throws <see cref="ArgumentNullException" /> when <paramref name="other" />
    /// is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Or_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = filter.Or(null!);
        });
    }

    // --------------------------------------------------------------------------------------
    // AllOf
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AllOf" /> passes the secondary gate when every supplied filter matches the date.
    /// </summary>
    [TestMethod]
    public void AllOf_WhenAllFiltersMatch_IsMatchReturnsTrue()
    {
        var filter = NotableDateFilter.AllOf(
            NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            NotableDateFilter.IsNonWorkingDay(),
            NotableDateFilter.WithTag("Public"));
        NotableDate date = MakeDate(
            category: NotableDateCategory.Holiday,
            isNonWorkingDay: true,
            tags: ImmutableHashSet.Create("Public"));

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AllOf" /> fails the secondary gate when any supplied filter does not match.
    /// </summary>
    [TestMethod]
    public void AllOf_WhenOneFilterFails_IsMatchReturnsFalse()
    {
        var filter = NotableDateFilter.AllOf(
            NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            NotableDateFilter.IsNonWorkingDay());
        NotableDate date = MakeDate(category: NotableDateCategory.Holiday, isNonWorkingDay: false);

        Assert.IsFalse(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AllOf" /> throws <see cref="ArgumentException" /> when the array is empty.
    /// </summary>
    [TestMethod]
    public void AllOf_WhenFiltersIsEmpty_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateFilter.AllOf();
        });
    }

    // --------------------------------------------------------------------------------------
    // AnyOf
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AnyOf" /> passes the secondary gate when at least one supplied filter matches.
    /// </summary>
    [TestMethod]
    public void AnyOf_WhenOneFilterMatches_IsMatchReturnsTrue()
    {
        var filter = NotableDateFilter.AnyOf(
            NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            NotableDateFilter.ForCategory(NotableDateCategory.Observance));
        NotableDate date = MakeDate(category: NotableDateCategory.Observance);

        Assert.IsTrue(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AnyOf" /> fails the secondary gate when no supplied filter matches.
    /// </summary>
    [TestMethod]
    public void AnyOf_WhenNoFilterMatches_IsMatchReturnsFalse()
    {
        var filter = NotableDateFilter.AnyOf(
            NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            NotableDateFilter.ForCategory(NotableDateCategory.Observance));
        NotableDate date = MakeDate(category: NotableDateCategory.Cultural);

        Assert.IsFalse(filter.IsMatch(date));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AnyOf" /> throws <see cref="ArgumentException" /> when the array is empty.
    /// </summary>
    [TestMethod]
    public void AnyOf_WhenFiltersIsEmpty_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateFilter.AnyOf();
        });
    }

    // --------------------------------------------------------------------------------------
    // Complex composition
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a complex And/Or composition evaluates correctly — (Holiday OR Observance) AND NonWorking AND has "Public" tag.
    /// </summary>
    [TestMethod]
    public void ComplexComposition_HolidayOrObservanceAndNonWorkingPublic_MatchesCorrectly()
    {
        NotableDateFilter filter = NotableDateFilter.AnyOf(
                NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
                NotableDateFilter.ForCategory(NotableDateCategory.Observance))
            .And(NotableDateFilter.IsNonWorkingDay())
            .And(NotableDateFilter.WithTag("Public"));

        NotableDate matching = MakeDate(
            category: NotableDateCategory.Observance,
            isNonWorkingDay: true,
            tags: ImmutableHashSet.Create("Public", "Religious"));

        NotableDate wrongCategory = MakeDate(
            category: NotableDateCategory.Cultural,
            isNonWorkingDay: true,
            tags: ImmutableHashSet.Create("Public"));

        NotableDate notNonWorking = MakeDate(
            category: NotableDateCategory.Holiday,
            isNonWorkingDay: false,
            tags: ImmutableHashSet.Create("Public"));

        Assert.IsTrue(filter.IsMatch(matching));
        Assert.IsFalse(filter.IsMatch(wrongCategory));
        Assert.IsFalse(filter.IsMatch(notNonWorking));
    }

    /// <summary>
    /// Verifies that combining a primary-capable filter with a date-level filter via Or always passes the primary gate (since one
    /// branch is always true at the rule level).
    /// </summary>
    [TestMethod]
    public void Or_WhenOneBranchIsDateLevelOnly_PrimaryGateAlwaysPassesForAnyRule()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Cultural)
            .Or(NotableDateFilter.WasAdjusted());

        NotableDateRule holidayRule = MakeRule(category: NotableDateCategory.Holiday);

        // The WasAdjusted branch always returns true at rule level, so the OR must also return true.
        Assert.IsTrue(filter.IsRuleEligible(holidayRule));
    }
}
