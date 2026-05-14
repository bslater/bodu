// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.ReEntrantGeneration.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that adjacent weekend holidays which both require forward substitution do not claim the same observed date.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenAdjacentWeekendHolidaysBothShiftForward_ShouldAllocateDistinctSubstituteDays()
    {
        // In 2021, Christmas Day was Saturday and Boxing Day was Sunday.
        // The expected substituted public holidays are Monday 27 December and Tuesday 28 December.
        NotableDateRule christmas = Fixed("Christmas Day", 12, 25, nonWorking: true) with
        {
            Adjustments = ImmutableArray.Create(CreateForwardSubstituteAdjustment("christmas-substitute")),
        };

        NotableDateRule boxingDay = Fixed("Boxing Day", 12, 26, nonWorking: true) with
        {
            Adjustments = ImmutableArray.Create(CreateForwardSubstituteAdjustment("boxing-day-substitute")),
        };

        NotableDateService service = BuildService(christmas, boxingDay);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2021);

        NotableDate christmasObserved = results.Single(date =>
            date.Name == "Christmas Day" &&
            date.WasAdjusted);

        NotableDate boxingDayObserved = results.Single(date =>
            date.Name == "Boxing Day" &&
            date.WasAdjusted);

        Assert.AreEqual(new DateTime(2021, 12, 27), christmasObserved.Date);
        Assert.AreEqual(new DateTime(2021, 12, 28), boxingDayObserved.Date);
        Assert.AreNotEqual(christmasObserved.Date, boxingDayObserved.Date);
    }

    /// <summary>
    /// Verifies that a forward-substitution adjustment treats base holidays from the same generated year as unavailable,
    /// even when the blocking base holiday appears later in provider order.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenForwardSubstituteCandidateIsBaseHolidayFromSameYear_ShouldSkipBaseHoliday()
    {
        // 5 January 2025 was Sunday. The first candidate substitute is Monday 6 January.
        // Monday 6 January is already a non-working base holiday, so the substitute must move to Tuesday 7 January.
        NotableDateRule sundayHoliday = Fixed("Sunday Holiday", 1, 5, nonWorking: true) with
        {
            Adjustments = ImmutableArray.Create(CreateForwardSubstituteAdjustment("sunday-substitute")),
        };

        NotableDateRule existingMondayHoliday = Fixed("Existing Monday Holiday", 1, 6, nonWorking: true);

        // Intentionally place the Sunday rule first. The generator must still see the later base holiday.
        NotableDateService service = BuildService(sundayHoliday, existingMondayHoliday);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        NotableDate observed = results.Single(date =>
            date.Name == "Sunday Holiday" &&
            date.WasAdjusted);

        Assert.AreEqual(new DateTime(2025, 1, 7), observed.Date);
    }

    /// <summary>
    /// Verifies that a forward-substitution adjustment treats every day in a same-year multi-day non-working span as unavailable.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenForwardSubstituteCandidateFallsInsideSameYearMultiDayHoliday_ShouldSkipEntireSpan()
    {
        // 5 January 2025 was Sunday. The substitute walk checks Monday 6 January first.
        // A same-year non-working span covers Monday 6 January and Tuesday 7 January.
        // The substitute should therefore land on Wednesday 8 January.
        NotableDateRule sundayHoliday = Fixed("Sunday Holiday", 1, 5, nonWorking: true) with
        {
            Adjustments = ImmutableArray.Create(CreateForwardSubstituteAdjustment("sunday-substitute")),
        };

        NotableDateRule blockingSpan = Fixed("Blocking Festival", 1, 6, nonWorking: true) with
        {
            DurationDays = 2,
        };

        NotableDateService service = BuildService(sundayHoliday, blockingSpan);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        NotableDate observed = results.Single(date =>
            date.Name == "Sunday Holiday" &&
            date.WasAdjusted);

        Assert.AreEqual(new DateTime(2025, 1, 8), observed.Date);
    }

    /// <summary>
    /// Verifies that an <see cref="AdjustmentTrigger.IfNonWorkingDay" /> adjustment can see non-working base dates
    /// from the same generation pass.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenIfNonWorkingDayReferencesSameYearBaseHoliday_ShouldActivate()
    {
        // 6 January 2025 was a Monday, so the trigger only activates if same-year base holidays are visible
        // during adjustment evaluation.
        NotableDateRule conditionalObservance = Fixed("Conditional Observance", 1, 6, nonWorking: false) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "if-non-working-day",
                Trigger = AdjustmentTrigger.IfNonWorkingDay,
                Action = AdjustmentAction.AddDays,
                OffsetDays = 1,
                IsNonWorkingDay = true,
            }),
        };

        NotableDateRule existingMondayHoliday = Fixed("Existing Monday Holiday", 1, 6, nonWorking: true);

        // Intentionally place the conditional rule first. The trigger must still see the later base holiday.
        NotableDateService service = BuildService(conditionalObservance, existingMondayHoliday);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        NotableDate? observed = results.SingleOrDefault(date =>
            date.Name == "Conditional Observance" &&
            date.WasAdjusted);

        Assert.IsNotNull(
            observed,
            "The IfNonWorkingDay adjustment should activate because 6 January is already a non-working base holiday.");

        Assert.AreEqual(new DateTime(2025, 1, 7), observed!.Date);
        Assert.AreEqual(AdjustmentTrigger.IfNonWorkingDay, observed.AdjustmentReason!.Trigger);
    }

    /// <summary>
    /// Verifies that a <see cref="AdjustmentAction.ReplaceWithNamedDate" /> adjustment can resolve a target rule
    /// from the same generation pass before the year has been cached.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenReplaceWithNamedDateTargetsSameYearRule_ShouldResolveTargetDuringGeneration()
    {
        NotableDateRule replacementSource = Fixed("Replacement Source", 1, 2) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "replace-with-target",
                Trigger = AdjustmentTrigger.Always,
                Action = AdjustmentAction.ReplaceWithNamedDate,
                TargetRuleName = "Target Holiday",
            }),
        };

        NotableDateRule targetHoliday = Fixed("Target Holiday", 7, 4);

        // Intentionally place the source rule first. The replacement must still resolve the later target rule.
        NotableDateService service = BuildService(replacementSource, targetHoliday);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        NotableDate? observed = results.SingleOrDefault(date =>
            date.Name == "Replacement Source" &&
            date.WasAdjusted);

        Assert.IsNotNull(
            observed,
            "ReplaceWithNamedDate should resolve the target rule from the in-progress generation context.");

        Assert.AreEqual(new DateTime(2025, 7, 4), observed!.Date);
        Assert.AreEqual(AdjustmentAction.ReplaceWithNamedDate, observed.AdjustmentReason!.Action);
    }

    /// <summary>
    /// Verifies that filtered year generation behaves consistently with full-year generation for same-year substitute blocking.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithFilter_WhenForwardSubstituteCandidateIsBaseHolidayFromSameYear_ShouldSkipBaseHoliday()
    {
        NotableDateRule sundayHoliday = Fixed("Sunday Holiday", 1, 5, nonWorking: true) with
        {
            Adjustments = ImmutableArray.Create(CreateForwardSubstituteAdjustment("sunday-substitute")),
        };

        NotableDateRule existingMondayHoliday = Fixed("Existing Monday Holiday", 1, 6, nonWorking: true);

        NotableDateService service = BuildService(sundayHoliday, existingMondayHoliday);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(
            2025,
            NotableDateFilter.WasAdjusted());

        NotableDate observed = results.Single(date => date.Name == "Sunday Holiday");

        Assert.AreEqual(new DateTime(2025, 1, 7), observed.Date);
        Assert.IsTrue(observed.WasAdjusted);
    }

    /// <summary>
    /// Verifies that same-year blocking preserves territory scope and does not treat unrelated territory holidays as unavailable.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenBlockingHolidayHasDifferentTerritory_ShouldNotBlockSubstitute()
    {
        // 5 January 2025 was Sunday. Monday 6 January is blocked only for AU.
        // A US-scoped substitute should not treat the AU holiday as unavailable.
        NotableDateRule usSundayHoliday = Fixed("US Sunday Holiday", 1, 5, nonWorking: true, territory: "US") with
        {
            Adjustments = ImmutableArray.Create(CreateForwardSubstituteAdjustment("us-sunday-substitute")),
        };

        NotableDateRule auMondayHoliday = Fixed("AU Monday Holiday", 1, 6, nonWorking: true, territory: "AU");

        NotableDateService service = BuildService(usSundayHoliday, auMondayHoliday);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025, territoryCode: "US");

        NotableDate observed = results.Single(date =>
            date.Name == "US Sunday Holiday" &&
            date.WasAdjusted);

        Assert.AreEqual(new DateTime(2025, 1, 6), observed.Date);
    }

    /// <summary>
    /// Returns known-answer cases for fixed-date holidays that require forward substitution.
    /// </summary>
    /// <returns>The known-answer test cases.</returns>
    private static IEnumerable<object[]> ForwardSubstituteKats()
    {
        yield return new object[]
        {
        new ForwardSubstituteKat(
            "Same-year adjacent weekend holidays should allocate Monday and Tuesday substitutes.",
            2021,
            new[]
            {
                Fixed("Christmas Day", 12, 25, nonWorking: true) with
                {
                    Adjustments = ImmutableArray.Create(CreateForwardSubstituteAdjustment("christmas-substitute")),
                },
                Fixed("Boxing Day", 12, 26, nonWorking: true) with
                {
                    Adjustments = ImmutableArray.Create(CreateForwardSubstituteAdjustment("boxing-day-substitute")),
                },
            },
            new[]
            {
                new ExpectedNotableDateKat("Christmas Day", new DateTime(2021, 12, 27), WasAdjusted: true),
                new ExpectedNotableDateKat("Boxing Day", new DateTime(2021, 12, 28), WasAdjusted: true),
            },
            new[]
            {
                new BlockedDateKat("Christmas Day", new DateTime(2021, 12, 28)),
                new BlockedDateKat("Boxing Day", new DateTime(2021, 12, 27)),
            }),
        };

        // TODO: Re-enable the cross-year known-answer case once the existing NotableDateService is routed through the new
        // RangeResolution pipeline (see follow-up to PR #188). The case relies on a MoveToNextNonWorkingDay walk crossing a
        // calendar-year boundary and observing a non-working holiday in the adjacent year, which the existing engine cannot do
        // because GetOrGenerateYear's re-entry guard blocks every cross-year materialisation while a year is being generated.
        // The new pipeline already produces the expected Jan 3 substitute — see
        // NotableDateRangePipelineTests.Resolve_WhenPriorYearHolidayRollsForwardPastBlocker_ShouldEmitObservedDateOnJanuaryThird.
        ////yield return new object[]
        ////{
        ////new ForwardSubstituteKat(
        ////    "Cross-year substitute should skip weekend and next-year fixed non-working holiday.",
        ////    2022,
        ////    new[]
        ////    {
        ////        Fixed("Year-End Holiday", 12, 31, nonWorking: true) with
        ////        {
        ////            Adjustments = ImmutableArray.Create(CreateForwardSubstituteAdjustment("year-end-substitute")),
        ////        },
        ////        Fixed("New Year Second Day", 1, 2, nonWorking: true) with
        ////        {
        ////            Adjustments = ImmutableArray.Create(CreateForwardSubstituteAdjustment("new-year-second-day-substitute")),
        ////        },
        ////    },
        ////    new[]
        ////    {
        ////        new ExpectedNotableDateKat("Year-End Holiday", new DateTime(2023, 1, 3), WasAdjusted: true),
        ////    },
        ////    new[]
        ////    {
        ////        new BlockedDateKat("Year-End Holiday", new DateTime(2023, 1, 2)),
        ////    }),
        ////};
    }

    public static string GetForwardSubstituteDisplayName(MethodInfo methodInfo, object[] data) =>
        ((ForwardSubstituteKat)data[0]).Name;

    /// <summary>
    /// Verifies that adjacent fixed-date holidays which require forward substitution are allocated distinct observed dates,
    /// including when the substitution crosses a calendar-year boundary.
    /// </summary>
    /// <param name="kat">The known-answer test case.</param>
    [TestMethod]
    [DynamicData(nameof(ForwardSubstituteKats), DynamicDataDisplayName = nameof(GetForwardSubstituteDisplayName))]
    public void GetNotableDates_WhenAdjacentFixedDateHolidaysShiftForward_ShouldAllocateDistinctSubstituteDays(
        ForwardSubstituteKat kat)
    {
        NotableDateService service = BuildService(kat.Rules.ToArray());

        IReadOnlyList<NotableDate> results = service.GetNotableDates(kat.QueryYear);

        foreach (ExpectedNotableDateKat expected in kat.ExpectedDates)
        {
            NotableDate actual = results.Single(date =>
                date.Name == expected.RuleName &&
                date.WasAdjusted == expected.WasAdjusted);

            Assert.AreEqual(
                expected.ExpectedDate,
                actual.Date,
                $"{kat.Name}: {expected.RuleName} should be emitted on the expected date.");
        }

        foreach (BlockedDateKat blocked in kat.BlockedDates)
        {
            Assert.IsFalse(
                results.Any(date =>
                    date.Name == blocked.RuleName &&
                    date.WasAdjusted &&
                    date.Date == blocked.BlockedDate),
                $"{kat.Name}: {blocked.RuleName} should not be emitted on blocked date {blocked.BlockedDate:yyyy-MM-dd}.");
        }
    }

    /// <summary>
    /// Creates a forward-substitution adjustment that walks forward until it finds the first available working day,
    /// which is then emitted as the observed non-working substitute.
    /// </summary>
    /// <param name="key">The adjustment key.</param>
    /// <returns>The adjustment.</returns>
    private static ObservanceAdjustment CreateForwardSubstituteAdjustment(string key) =>
        new()
        {
            Key = key,
            Trigger = AdjustmentTrigger.IfWeekend,
            Action = AdjustmentAction.MoveToNextNonWorkingDay,
            IsNonWorkingDay = true,
        };

    /// <summary>
    /// Represents an expected generated notable date for a known-answer test case.
    /// </summary>
    /// <param name="RuleName">The expected rule/name.</param>
    /// <param name="ExpectedDate">The expected generated date.</param>
    /// <param name="WasAdjusted">The expected adjustment state.</param>
    public sealed record ExpectedNotableDateKat(
        string RuleName,
        DateTime ExpectedDate,
        bool WasAdjusted);

    /// <summary>
    /// Represents a date that must not be selected by a specified rule during forward-substitution calculation.
    /// </summary>
    /// <param name="RuleName">The rule/name being checked.</param>
    /// <param name="BlockedDate">The date that must not be selected.</param>
    public sealed record BlockedDateKat(
        string RuleName,
        DateTime BlockedDate);

    /// <summary>
    /// Represents a known-answer case for fixed-date holidays that use forward substitution.
    /// </summary>
    /// <param name="Name">The scenario name.</param>
    /// <param name="QueryYear">The year requested from the service.</param>
    /// <param name="Rules">The notable-date rules used by the scenario.</param>
    /// <param name="ExpectedDates">The expected generated dates.</param>
    /// <param name="BlockedDates">Dates that must not be selected by specific adjusted rules.</param>
    public sealed record ForwardSubstituteKat(
        string Name,
        int QueryYear,
        IReadOnlyList<NotableDateRule> Rules,
        IReadOnlyList<ExpectedNotableDateKat> ExpectedDates,
        IReadOnlyList<BlockedDateKat> BlockedDates)
    {
        public override string ToString() => Name;
    }
}