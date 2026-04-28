// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.Build.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.Build" /> returns a single <see cref="NotableDateRule" />
    /// with all Fixed-strategy properties correctly populated when a single fixed rule is defined.
    /// </summary>
    [TestMethod]
    public void Build_WhenSingleFixedRule_ShouldReturnCorrectRule()
    {
        IReadOnlyList<NotableDateRule> rules = NotableDateDocumentBuilder.Create()
            .AddDate("Christmas Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .NonWorking()
                    .Fixed(12, 25)))
            .Build();

        Assert.AreEqual(1, rules.Count);
        NotableDateRule rule = rules[0];
        Assert.AreEqual("Christmas Day", rule.Name);
        Assert.AreEqual(DateResolutionStrategy.Fixed, rule.Strategy);
        Assert.AreEqual(NotableDateCategory.Holiday, rule.Category);
        Assert.AreEqual(true, rule.IsNonWorkingDay);
        Assert.AreEqual(12, rule.Month);
        Assert.AreEqual(25, rule.Day);
    }

    /// <summary>
    /// Verifies that an adjustment added via <see cref="NotableDateRuleBuilder.AddAdjustment" /> is present on
    /// the built <see cref="NotableDateRule" /> with all properties correctly set.
    /// </summary>
    [TestMethod]
    public void Build_WhenFixedRuleWithAdjustment_ShouldReturnAdjustmentOnRule()
    {
        IReadOnlyList<NotableDateRule> rules = NotableDateDocumentBuilder.Create()
            .AddDate("New Year's Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .NonWorking()
                    .Fixed(1, 1)
                    .AddAdjustment("weekend-roll", adj => adj
                        .When(AdjustmentTrigger.IfWeekend)
                        .Action(AdjustmentAction.MoveToNextWeekday)
                        .NonWorking())))
            .Build();

        Assert.AreEqual(1, rules.Count);
        ImmutableArray<ObservanceAdjustment> adjustments = rules[0].Adjustments;
        Assert.AreEqual(1, adjustments.Length);
        Assert.AreEqual("weekend-roll", adjustments[0].Key);
        Assert.AreEqual(AdjustmentTrigger.IfWeekend, adjustments[0].Trigger);
        Assert.AreEqual(AdjustmentAction.MoveToNextWeekday, adjustments[0].Action);
        Assert.AreEqual(true, adjustments[0].IsNonWorkingDay);
    }

    /// <summary>
    /// Verifies that the <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> strategy fields are correctly
    /// populated on the built rule when a day-of-week-in-month strategy is selected.
    /// </summary>
    [TestMethod]
    public void Build_WhenDayOfWeekInMonthStrategy_ShouldReturnCorrectStrategyFields()
    {
        IReadOnlyList<NotableDateRule> rules = NotableDateDocumentBuilder.Create()
            .AddDate("Labour Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .NonWorking()
                    .DayOfWeekInMonth(10, DayOfWeek.Monday, WeekOfMonthOrdinal.Second)))
            .Build();

        Assert.AreEqual(1, rules.Count);
        NotableDateRule rule = rules[0];
        Assert.AreEqual(DateResolutionStrategy.DayOfWeekInMonth, rule.Strategy);
        Assert.AreEqual(10, rule.Month);
        Assert.AreEqual(DayOfWeek.Monday, rule.DayOfWeek);
        Assert.AreEqual(WeekOfMonthOrdinal.Second, rule.WeekOrdinal);
    }

    /// <summary>
    /// Verifies that the <see cref="DateResolutionStrategy.OffsetFromAnchor" /> strategy fields are correctly
    /// populated on the built rule when an offset-from-anchor strategy is selected.
    /// </summary>
    [TestMethod]
    public void Build_WhenOffsetFromAnchorStrategy_ShouldReturnCorrectStrategyFields()
    {
        IReadOnlyList<NotableDateRule> rules = NotableDateDocumentBuilder.Create()
            .AddDate("Easter Monday", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .NonWorking()
                    .OffsetFromAnchor("Easter Sunday", 1)))
            .Build();

        Assert.AreEqual(1, rules.Count);
        NotableDateRule rule = rules[0];
        Assert.AreEqual(DateResolutionStrategy.OffsetFromAnchor, rule.Strategy);
        Assert.AreEqual("Easter Sunday", rule.AnchorRuleName);
        Assert.AreEqual(1, rule.OffsetDays);
    }

    /// <summary>
    /// Verifies that the <see cref="DateResolutionStrategy.Algorithm" /> strategy fields are correctly populated
    /// on the built rule when an algorithm key is supplied.
    /// </summary>
    [TestMethod]
    public void Build_WhenAlgorithmStrategy_ShouldReturnCorrectStrategyFields()
    {
        IReadOnlyList<NotableDateRule> rules = NotableDateDocumentBuilder.Create()
            .AddDate("Easter Sunday", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .Algorithm(key: "easter-gregorian")))
            .Build();

        Assert.AreEqual(1, rules.Count);
        Assert.AreEqual(DateResolutionStrategy.Algorithm, rules[0].Strategy);
        Assert.AreEqual("easter-gregorian", rules[0].AlgorithmKey);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.Build" /> returns rules from all added notable date entries
    /// in insertion order when multiple dates are added.
    /// </summary>
    [TestMethod]
    public void Build_WhenMultipleDatesAdded_ShouldReturnAllRulesInOrder()
    {
        IReadOnlyList<NotableDateRule> rules = NotableDateDocumentBuilder.Create()
            .AddDate("Christmas Day", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday).Fixed(12, 25)))
            .AddDate("Boxing Day", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday).Fixed(12, 26)))
            .Build();

        Assert.AreEqual(2, rules.Count);
        Assert.AreEqual("Christmas Day", rules[0].Name);
        Assert.AreEqual("Boxing Day", rules[1].Name);
    }

    /// <summary>
    /// Verifies that multiple rules for the same notable date all receive the same canonical name and their individual
    /// <see cref="NotableDateRule.RuleName" /> values are set correctly.
    /// </summary>
    [TestMethod]
    public void Build_WhenMultipleRulesForSameDate_ShouldShareNameAndPreserveRuleNames()
    {
        IReadOnlyList<NotableDateRule> rules = NotableDateDocumentBuilder.Create()
            .AddDate("King's Birthday", date => date
                .AddRule(rule => rule
                    .RuleName("queensland")
                    .Category(NotableDateCategory.Holiday)
                    .Territory("AU-QLD")
                    .DayOfWeekInMonth(10, DayOfWeek.Monday, WeekOfMonthOrdinal.First))
                .AddRule(rule => rule
                    .RuleName("other-states")
                    .Category(NotableDateCategory.Holiday)
                    .Territory("AU-NSW,AU-VIC")
                    .DayOfWeekInMonth(6, DayOfWeek.Monday, WeekOfMonthOrdinal.Second)))
            .Build();

        Assert.AreEqual(2, rules.Count);
        Assert.AreEqual("King's Birthday", rules[0].Name);
        Assert.AreEqual("King's Birthday", rules[1].Name);
        Assert.AreEqual("queensland", rules[0].RuleName);
        Assert.AreEqual("other-states", rules[1].RuleName);
    }

    /// <summary>
    /// Verifies that optional scalar rule properties — territory, firstYear, lastYear, durationDays, priority, comment,
    /// and tags — are correctly propagated to the built rule.
    /// </summary>
    [TestMethod]
    public void Build_WhenRuleHasOptionalScalars_ShouldPopulateAllFields()
    {
        IReadOnlyList<NotableDateRule> rules = NotableDateDocumentBuilder.Create()
            .AddDate("Test Date", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Observance)
                    .Territory("AU")
                    .FirstYear(2000)
                    .LastYear(2099)
                    .Duration(3)
                    .Priority(50)
                    .Comment("Test comment")
                    .AddTag("TestTag")
                    .Fixed(7, 4)))
            .Build();

        Assert.AreEqual(1, rules.Count);
        NotableDateRule rule = rules[0];
        Assert.AreEqual("AU", rule.TerritoryCode);
        Assert.AreEqual(2000, rule.FirstYear);
        Assert.AreEqual(2099, rule.LastYear);
        Assert.AreEqual(3, rule.DurationDays);
        Assert.AreEqual(50, rule.Priority);
        Assert.AreEqual("Test comment", rule.Comment);
        Assert.IsTrue(rule.Tags.Contains("TestTag"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.Build" /> throws <see cref="InvalidOperationException" />
    /// when a rule builder has no strategy selected.
    /// </summary>
    [TestMethod]
    public void Build_WhenNoStrategySet_ShouldThrowInvalidOperationException()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Missing Strategy", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday)));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.Build();
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.Build" /> throws <see cref="InvalidOperationException" />
    /// when a notable date entry has no rules added.
    /// </summary>
    [TestMethod]
    public void Build_WhenNoRulesForDate_ShouldThrowInvalidOperationException()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Empty Date", _ => { });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.Build();
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.AddDate" /> throws <see cref="ArgumentNullException" />
    /// when the <c>name</c> argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddDate_WhenNameIsNull_ShouldThrowArgumentNullException()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddDate(null!, _ => { });
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.AddDate" /> throws <see cref="ArgumentNullException" />
    /// when the <c>configure</c> callback is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddDate_WhenConfigureIsNull_ShouldThrowArgumentNullException()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddDate("Some Date", null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.ToProvider" /> returns an <see cref="INotableDateRuleProvider" />
    /// whose <see cref="INotableDateRuleProvider.LoadRules" /> output matches the rules returned by
    /// <see cref="NotableDateDocumentBuilder.Build" />.
    /// </summary>
    [TestMethod]
    public void ToProvider_WhenBuilderIsValid_ShouldReturnProviderWithSameRules()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Christmas Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(12, 25)));

        IReadOnlyList<NotableDateRule> builtRules = builder.Build();
        INotableDateRuleProvider provider = builder.ToProvider();
        List<NotableDateRule> providerRules = provider.LoadRules().ToList();

        Assert.AreEqual(builtRules.Count, providerRules.Count);
        Assert.AreEqual(builtRules[0].Name, providerRules[0].Name);
        Assert.AreEqual(builtRules[0].Month, providerRules[0].Month);
        Assert.AreEqual(builtRules[0].Day, providerRules[0].Day);
    }

    /// <summary>
    /// Verifies that the string overload of <see cref="NotableDateRuleBuilder.Fixed(string, int, bool, bool)" />
    /// correctly resolves an English month name to a numeric month on the built rule.
    /// </summary>
    [TestMethod]
    public void Build_WhenFixedWithStringMonthToken_ShouldResolveGregorianMonthCorrectly()
    {
        IReadOnlyList<NotableDateRule> rules = NotableDateDocumentBuilder.Create()
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed("March", 17)))
            .Build();

        Assert.AreEqual(1, rules.Count);
        Assert.AreEqual(3, rules[0].Month);
        Assert.IsNull(rules[0].CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that a leap-year-dependent Hebrew month alias token is stored in
    /// <see cref="NotableDateRule.CalendarMonthAlias" /> and leaves <see cref="NotableDateRule.Month" />
    /// <see langword="null" /> on the built rule.
    /// </summary>
    [TestMethod]
    public void Build_WhenFixedWithHebrewAliasToken_ShouldSetCalendarMonthAlias()
    {
        IReadOnlyList<NotableDateRule> rules = NotableDateDocumentBuilder.Create()
            .AddDate("Passover", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .Fixed("Nisan", 15)))
            .Build();

        Assert.AreEqual(1, rules.Count);
        Assert.IsNull(rules[0].Month);
        Assert.AreEqual("Nisan", rules[0].CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that territory scoping set on an <see cref="ObservanceAdjustmentBuilder" /> is correctly
    /// propagated to the built <see cref="ObservanceAdjustment" /> along with other adjustment properties.
    /// </summary>
    [TestMethod]
    public void Build_WhenAdjustmentHasTerritoryScope_ShouldPopulateTerritoryOnAdjustment()
    {
        IReadOnlyList<NotableDateRule> rules = NotableDateDocumentBuilder.Create()
            .AddDate("ANZAC Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Remembrance)
                    .Fixed(4, 25)
                    .AddAdjustment("anzac-wa-sub", adj => adj
                        .When(AdjustmentTrigger.IfDayOfWeek)
                        .OnDayOfWeek(DayOfWeek.Sunday)
                        .Action(AdjustmentAction.MoveToNextWeekday)
                        .Territory("AU-WA")
                        .Priority(10))))
            .Build();

        ObservanceAdjustment adj = rules[0].Adjustments[0];
        Assert.AreEqual("AU-WA", adj.TerritoryCode);
        Assert.AreEqual(DayOfWeek.Sunday, adj.DayOfWeek);
        Assert.AreEqual(10, adj.Priority);
    }
}
