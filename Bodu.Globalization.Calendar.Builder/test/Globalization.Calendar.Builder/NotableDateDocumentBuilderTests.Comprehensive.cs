// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.Comprehensive.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// Builds a document exercising the breadth of the fluent authoring surface: every resolution-policy component,
    /// every adjustment-trigger/action shape, scope filters, imports with uses, overrides, and the full range of rule
    /// strategies and per-rule attributes.
    /// </summary>
    /// <returns>The comprehensive document builder.</returns>
    private static NotableDateDocumentBuilder ComprehensiveDocument() =>
        NotableDateDocumentBuilder.Create("demo.comprehensive")
            .WithMetadata(name: "Comprehensive", description: "Every feature.", "tester")
            .WithResolutionPolicy(p => p
                .WithDuplicatePolicy(RangeResolution.DuplicatePolicy.KeepLast)
                .WithSameDayCollisionPolicy(RangeResolution.CollisionPolicy.HighestPriorityOnly)
                .WithSpanCollisionPolicy(RangeResolution.CollisionPolicy.CategoryPriority)
                .WithPriorityDirection(RangeResolution.PriorityDirection.LowerWins)
                .WithObservedDateRangePolicy(RangeResolution.ObservedDateRangePolicy.BothOccurrencesControlInclusion)
                .WithWorkingWeek(WeekPattern.MondayToFriday)
                .WithCategoryPrecedence(NotableDateCategory.PublicHoliday, NotableDateCategory.Religious, NotableDateCategory.Observance))
            .AddAdjustmentPolicy("adddays", a => a
                .WithDescription("add days")
                .WithPriority(50)
                .When(AdjustmentTrigger.IfWeekend)
                .OnTriggerWeekdays(DayOfWeek.Saturday, DayOfWeek.Sunday)
                .Then(AdjustmentAction.AddDays)
                .WithActionDays(2)
                .Emit(RangeResolution.EmissionMode.ActualAndObserved)
                .WithReason("added")
                .EmitNonWorking(true))
            .AddAdjustmentPolicy("mondayise", a => a
                .When(AdjustmentTrigger.IfWeekend)
                .Then(AdjustmentAction.MoveToNextWeekday)
                .WithActionDayOfWeek(DayOfWeek.Monday)
                .SkipWeekends(true)
                .SkipNonWorkingDates(true)
                .WithMaxSearchDays(7)
                .Emit(RangeResolution.EmissionMode.ObservedOnly)
                .WithReason("mon"))
            .AddAdjustmentPolicy("replace", a => a
                .When(AdjustmentTrigger.IfDayOfWeek)
                .OnTriggerWeekdays(DayOfWeek.Sunday)
                .Then(AdjustmentAction.ReplaceWithRule)
                .WithReplacementRule("easter", "default")
                .Emit(RangeResolution.EmissionMode.ActualAndObserved))
            .AddAdjustmentPolicy("custom", a => a
                .When(AdjustmentTrigger.IfBeforeFixedDate)
                .WithTriggerMonth(6)
                .WithTriggerDay(15)
                .Then(AdjustmentAction.Custom)
                .WithActionHandlerKey("handler")
                .WithParameter("k1", "v1")
                .WithParameter("k2", "v2")
                .Emit(RangeResolution.EmissionMode.Suppress))
            .AddAdjustmentPolicy("nth", a => a
                .When(AdjustmentTrigger.IfNthOccurrenceInMonth)
                .WithTriggerWeekOrdinal(WeekOrdinal.Second)
                .Then(AdjustmentAction.MoveToPreviousWorkingDay)
                .Emit(RangeResolution.EmissionMode.ActualOnly))
            .AddAdjustmentPolicy("trigger-handler", a => a
                .When(AdjustmentTrigger.Custom)
                .WithTriggerHandlerKey("trig")
                .Then(AdjustmentAction.MoveToPreviousWeekday)
                .WithActionDayOfWeek(DayOfWeek.Friday)
                .Emit(RangeResolution.EmissionMode.ObservedOnly)
                .WithReason("x"))
            .AddNotableDate("new-year", "New Year", NotableDateCategory.PublicHoliday, d => d
                .AsNonWorkingByDefault()
                .WithDisplayName("New Year's Day")
                .WithCategory(NotableDateCategory.PublicHoliday)
                .AddTag("major")
                .WithTags("national", "bank")
                .WithDefaultDurationDays(1)
                .AddRule("default", r => r
                    .ForTerritory("US")
                    .ForCalendar(CalendarSystem.Gregorian)
                    .Fixed(1, 1)
                    .WithPriority(100)
                    .WithCategory(NotableDateCategory.PublicHoliday)
                    .AsNonWorking(true)
                    .WithDurationDays(1)
                    .WithComment("comment")
                    .AddTag("t")
                    .WithTags("a", "b")
                    .FromYear(2000)
                    .ToYear(2100)
                    .WithAdjustment("mondayise"))
                .AddRule("periodic", r => r
                    .ForTerritories("US", "CA")
                    .EveryYears(4)
                    .AnchorYear(2024)
                    .Fixed(2, 29)
                    .WithAdjustments("adddays", "mondayise"))
                .AddRule("only-except", r => r
                    .ForTerritory("GB")
                    .Fixed("March", 1)
                    .OnlyYears(2025, 2026)
                    .ExceptYears(2027)))
            .AddNotableDate("thanksgiving", "Thanksgiving", NotableDateCategory.PublicHoliday, d => d
                .AddRule("default", r => r.ForTerritory("US").DayOfWeekInMonth(11, DayOfWeek.Thursday, WeekOrdinal.Fourth)))
            .AddNotableDate("easter", "Easter", NotableDateCategory.Religious, d => d
                .AddRule("default", r => r.Algorithm("western-easter")))
            .AddNotableDate("good-friday", "Good Friday", NotableDateCategory.Religious, d => d
                .AddRule("default", r => r.OffsetFromRule("easter", -2, "default")))
            .AddNotableDate("near", "Near", NotableDateCategory.Observance, d => d
                .AddRule("default", r => r.WeekdayNearDate(5, 24, DayOfWeek.Monday, WeekdayProximity.OnOrBefore)))
            .AddNotableDate("relative", "Relative", NotableDateCategory.Observance, d => d
                .AddRule("default", r => r.RelativeWeekdayInMonth(11, DayOfWeek.Thursday, WeekOrdinal.Fourth, DayOfWeek.Friday, WeekdayProximity.After)))
            .AddOverride(o => o
                .AddRule("thanksgiving", "extra", r => r.ForTerritory("CA").Fixed(10, 8))
                .PatchRule("new-year", "default", r => r.WithPriority(200))
                .RemoveRule("new-year", "only-except"));

    /// <summary>
    /// Verifies that the comprehensive document round-trips through XML: re-parsing its serialized form and serializing
    /// again yields identical XML.
    /// </summary>
    [TestMethod]
    public void Comprehensive_ShouldRoundTripThroughXml()
    {
        var xml = ComprehensiveDocument().ToXml();

        var reserialized = NotableDateDocumentBuilder.FromXml(xml).ToXml();

        Assert.AreEqual(xml, reserialized);
    }

    /// <summary>
    /// Verifies that the comprehensive document round-trips through JSON: re-parsing its serialized form and
    /// serializing again yields identical JSON.
    /// </summary>
    /// <summary>
    /// Builds a document using only the features the JSON subset can represent: metadata, resolution policy, plain
    /// adjustment policies, the full range of rule strategies and attributes, and overrides.
    /// </summary>
    /// <returns>The JSON-compatible document builder.</returns>
    private static NotableDateDocumentBuilder JsonSafeDocument() =>
        NotableDateDocumentBuilder.Create("demo.json")
            .WithMetadata(name: "Json", description: "Json subset.", "tester")
            .WithResolutionPolicy(p => p
                .WithDuplicatePolicy(RangeResolution.DuplicatePolicy.Merge)
                .WithSameDayCollisionPolicy(RangeResolution.CollisionPolicy.HighestPriorityOnly)
                .WithSpanCollisionPolicy(RangeResolution.CollisionPolicy.CategoryPriority)
                .WithPriorityDirection(RangeResolution.PriorityDirection.LowerWins)
                .WithObservedDateRangePolicy(RangeResolution.ObservedDateRangePolicy.ActualOccurrenceControlsInclusion)
                .WithWorkingWeek(WeekPattern.MondayToFriday)
                .WithCategoryPrecedence(NotableDateCategory.PublicHoliday, NotableDateCategory.Religious))
            .AddAdjustmentPolicy("adddays", a => a
                .WithDescription("add days")
                .WithPriority(50)
                .When(AdjustmentTrigger.IfWeekend)
                .OnTriggerWeekdays(DayOfWeek.Saturday, DayOfWeek.Sunday)
                .Then(AdjustmentAction.AddDays)
                .WithActionDays(2)
                .Emit(RangeResolution.EmissionMode.ActualAndObserved)
                .WithReason("added")
                .EmitNonWorking(true))
            .AddAdjustmentPolicy("mondayise", a => a
                .When(AdjustmentTrigger.IfWeekend)
                .Then(AdjustmentAction.MoveToNextWeekday)
                .WithActionDayOfWeek(DayOfWeek.Monday)
                .SkipWeekends(true)
                .SkipNonWorkingDates(true)
                .WithMaxSearchDays(7)
                .Emit(RangeResolution.EmissionMode.ObservedOnly)
                .WithReason("mon"))
            .AddNotableDate("new-year", "New Year", NotableDateCategory.PublicHoliday, d => d
                .AsNonWorkingByDefault()
                .WithDisplayName("New Year's Day")
                .WithCategory(NotableDateCategory.PublicHoliday)
                .AddTag("major")
                .WithTags("national", "bank")
                .WithDefaultDurationDays(1)
                .AddRule("default", r => r
                    .ForTerritory("US")
                    .ForCalendar(CalendarSystem.Gregorian)
                    .Fixed(1, 1)
                    .WithPriority(100)
                    .WithCategory(NotableDateCategory.PublicHoliday)
                    .AsNonWorking(true)
                    .WithDurationDays(1)
                    .WithComment("comment")
                    .AddTag("t")
                    .WithTags("a", "b")
                    .FromYear(2000)
                    .ToYear(2100)
                    .WithAdjustment("mondayise"))
                .AddRule("periodic", r => r
                    .ForTerritories("US", "CA")
                    .EveryYears(4)
                    .AnchorYear(2024)
                    .Fixed(2, 29)
                    .WithAdjustments("adddays", "mondayise"))
                .AddRule("only-except", r => r
                    .ForTerritory("GB")
                    .Fixed("March", 1)
                    .OnlyYears(2025, 2026)
                    .ExceptYears(2027)))
            .AddNotableDate("thanksgiving", "Thanksgiving", NotableDateCategory.PublicHoliday, d => d
                .AddRule("default", r => r.ForTerritory("US").DayOfWeekInMonth(11, DayOfWeek.Thursday, WeekOrdinal.Fourth)))
            .AddNotableDate("easter", "Easter", NotableDateCategory.Religious, d => d
                .AddRule("default", r => r.Algorithm("western-easter")))
            .AddNotableDate("good-friday", "Good Friday", NotableDateCategory.Religious, d => d
                .AddRule("default", r => r.OffsetFromRule("easter", -2, "default")))
            .AddNotableDate("near", "Near", NotableDateCategory.Observance, d => d
                .AddRule("default", r => r.WeekdayNearDate(5, 24, DayOfWeek.Monday, WeekdayProximity.OnOrBefore)))
            .AddNotableDate("relative", "Relative", NotableDateCategory.Observance, d => d
                .AddRule("default", r => r.RelativeWeekdayInMonth(11, DayOfWeek.Thursday, WeekOrdinal.Fourth, DayOfWeek.Friday, WeekdayProximity.After)))
            .AddOverride(o => o
                .AddRule("thanksgiving", "extra", r => r.ForTerritory("CA").Fixed(10, 8))
                .PatchRule("new-year", "default", r => r.WithPriority(200))
                .RemoveRule("new-year", "only-except"));

    [TestMethod]
    public void Comprehensive_ShouldRoundTripThroughJson()
    {
        var json = JsonSafeDocument().ToJson();

        var reserialized = NotableDateDocumentBuilder.FromJson(json).ToJson();

        Assert.AreEqual(json, reserialized);
    }

    /// <summary>
    /// Builds a small document that uses imports with the full <see cref="ImportUseBuilder" /> surface.
    /// </summary>
    /// <returns>The import-bearing document builder.</returns>
    private static NotableDateDocumentBuilder ImportingDocument() =>
        NotableDateDocumentBuilder.Create("demo.imports")
            .AddAdjustmentPolicy("mondayise", a => a
                .When(AdjustmentTrigger.IfWeekend)
                .Then(AdjustmentAction.MoveToNextWorkingDay)
                .Emit(RangeResolution.EmissionMode.ObservedOnly)
                .WithScope(s => s
                    .ForTerritory("US")
                    .ForCalendar(CalendarSystem.Gregorian)
                    .ForCategory(NotableDateCategory.PublicHoliday)
                    .ForNotableDate("local")
                    .ForRule("local", "default")
                    .FromYear(2000)
                    .ToYear(2100)
                    .OnlyYear(2025)
                    .ExceptYear(2026)))
            .AddImport("global-core", i => i
                .Use("christmas", u => u
                    .ForTerritory("GB")
                    .As("xmas")
                    .AsNonWorking()
                    .WithAdjustment("mondayise")
                    .WithCategory(NotableDateCategory.PublicHoliday)))
            .AddNotableDate("local", "Local", NotableDateCategory.Observance, d => d
                .AddRule("default", r => r.Fixed(6, 1)));

    /// <summary>
    /// Verifies that an import-bearing document round-trips through XML, exercising the import and import-use
    /// serialization that the JSON subset cannot represent.
    /// </summary>
    [TestMethod]
    public void Imports_ShouldRoundTripThroughXml()
    {
        var xml = ImportingDocument().ToXml();

        Assert.AreEqual(xml, NotableDateDocumentBuilder.FromXml(xml).ToXml());
    }
}
