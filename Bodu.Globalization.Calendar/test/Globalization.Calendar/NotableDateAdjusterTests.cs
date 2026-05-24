// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAdjusterTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateAdjuster" /> evaluates every <see cref="AdjustmentTrigger" /> and applies every
/// <see cref="AdjustmentAction" />.
/// </summary>
[TestClass]
public sealed partial class NotableDateAdjusterTests
{
    private static NotableDateRule SampleRule(string name = "Test") => new()
    {
        Name = name,
        Strategy = DateResolutionStrategy.Fixed,
        Category = NotableDateCategory.Holiday,
    };

    private static NotableDateAdjuster CreateAdjuster(
        Func<DateTime, bool>? isWeekend = null,
        Func<DateTime, string?, Type?, bool>? isNonWorking = null,
        IAdjustmentHandlerRegistry? handlers = null,
        Func<string, int, string?, Type?, DateTime?>? resolveByName = null)
    {
        return new NotableDateAdjuster(
            isWeekend ?? (d => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday),
            isNonWorking ?? ((d, t, c) => false),
            WeekPattern.Weekdays,
            handlerRegistry: handlers,
            resolveByName: resolveByName);
    }

    /// <summary>
    /// Verifies that the <see cref="AdjustmentTrigger.Always" /> trigger always activates and that
    /// <see cref="AdjustmentAction.AddDays" /> shifts the date by the configured offset.
    /// </summary>
    [TestMethod]
    public void Apply_WhenAlwaysAddDays_ShouldShiftDate()
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.AddDays,
            OffsetDays = 3,
        };

        var result = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 1, 1));

        Assert.IsTrue(result.Activated);
        Assert.AreEqual(new DateTime(2025, 1, 4), result.AdjustedDate);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfWeekend" /> with <see cref="AdjustmentAction.MoveToNextWeekday" /> rolls a Saturday
    /// holiday onto Monday.
    /// </summary>
    [TestMethod]
    public void Apply_WhenSaturdayAndIfWeekend_ShouldMoveToMonday()
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.IfWeekend,
            Action = AdjustmentAction.MoveToNextWeekday,
        };

        var saturday = new DateTime(2026, 1, 3);
        var result = adjuster.Apply(adjustment, SampleRule(), saturday);

        Assert.IsTrue(result.Activated);
        Assert.AreEqual(DayOfWeek.Monday, result.AdjustedDate.DayOfWeek);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfWeekend" /> does not fire on a Tuesday.
    /// </summary>
    [TestMethod]
    public void Apply_WhenTuesdayAndIfWeekend_ShouldNotActivate()
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.IfWeekend,
            Action = AdjustmentAction.MoveToNextWeekday,
        };

        var result = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 1, 7));

        Assert.IsFalse(result.Activated);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfDayOfWeek" /> only activates when the date matches the configured weekday.
    /// </summary>
    [TestMethod]
    public void Apply_WhenIfDayOfWeekMatches_ShouldActivate()
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.IfDayOfWeek,
            DayOfWeek = DayOfWeek.Friday,
            Action = AdjustmentAction.AddDays,
            OffsetDays = 1,
        };

        var friday = new DateTime(2025, 1, 3);
        var result = adjuster.Apply(adjustment, SampleRule(), friday);

        Assert.IsTrue(result.Activated);
        Assert.AreEqual(new DateTime(2025, 1, 4), result.AdjustedDate);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfNonWorkingDay" /> consults the supplied predicate.
    /// </summary>
    [TestMethod]
    public void Apply_WhenIfNonWorkingDayPredicateReturnsTrue_ShouldActivate()
    {
        var adjuster = CreateAdjuster(isNonWorking: (d, t, c) => true);
        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.IfNonWorkingDay,
            Action = AdjustmentAction.AddDays,
            OffsetDays = 2,
        };

        var result = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 1, 1));

        Assert.IsTrue(result.Activated);
        Assert.AreEqual(new DateTime(2025, 1, 3), result.AdjustedDate);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfLeapYear" /> only activates in leap years.
    /// </summary>
    [TestMethod]
    public void Apply_WhenIfLeapYearAndYearIsLeap_ShouldActivate()
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.IfLeapYear,
            Action = AdjustmentAction.None,
        };

        var leap = adjuster.Apply(adjustment, SampleRule(), new DateTime(2024, 2, 29));
        var common = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 2, 28));

        Assert.IsTrue(leap.Activated);
        Assert.IsFalse(common.Activated);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> reprojects the comparison month/day onto the active year.
    /// </summary>
    [TestMethod]
    public void Apply_WhenIfBeforeFixedDate_ShouldUseProjectedYear()
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.IfBeforeFixedDate,
            Action = AdjustmentAction.AddDays,
            OffsetDays = 7,
            ComparisonDate = new DateTime(2000, 6, 1),
        };

        var early = adjuster.Apply(adjustment, SampleRule(), new DateTime(2030, 5, 10));
        var late = adjuster.Apply(adjustment, SampleRule(), new DateTime(2030, 6, 10));

        Assert.IsTrue(early.Activated);
        Assert.IsFalse(late.Activated);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" /> activates only when the date is the nth weekday of its
    /// month.
    /// </summary>
    [TestMethod]
    public void Apply_WhenIfNthOccurrenceInMonth_ShouldRespectOrdinal()
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.IfNthOccurrenceInMonth,
            Action = AdjustmentAction.None,
            WeekOrdinal = WeekOfMonthOrdinal.Second,
        };

        // 8 January 2025 is the second day-of-week-ordinal slot.
        var second = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 1, 8));
        var first = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 1, 1));

        Assert.IsTrue(second.Activated);
        Assert.IsFalse(first.Activated);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> skips days that are already non-working and
    /// stops on the first working day, which becomes the substitute observance.
    /// </summary>
    [TestMethod]
    public void Apply_WhenMoveToNextNonWorkingDay_ShouldSkipNonWorkingDaysAndStopOnFirstWorkingDay()
    {
        // isNonWorking returns true for Sat/Sun. Walk starts from a Saturday (4 Jan 2025);
        // the next cursor is Sunday (non-working, skipped), then Monday (working) — the first eligible day.
        var adjuster = CreateAdjuster(
            isNonWorking: (d, t, c) => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);

        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.MoveToNextNonWorkingDay,
        };

        var saturday = new DateTime(2025, 1, 4); // Sat
        var result = adjuster.Apply(adjustment, SampleRule(), saturday);

        Assert.IsTrue(result.Activated);
        Assert.AreEqual(DayOfWeek.Monday, result.AdjustedDate.DayOfWeek);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.ReplaceWithNamedDate" /> uses the supplied name resolver callback.
    /// </summary>
    [TestMethod]
    public void Apply_WhenReplaceWithNamedDate_ShouldUseResolverCallback()
    {
        var target = new DateTime(2025, 12, 26);
        var adjuster = CreateAdjuster(resolveByName: (name, year, t, c) =>
            string.Equals(name, "Boxing Day", StringComparison.OrdinalIgnoreCase) ? target : null);

        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.ReplaceWithNamedDate,
            TargetRuleName = "Boxing Day",
        };

        var result = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 12, 25));

        Assert.IsTrue(result.Activated);
        Assert.AreEqual(target, result.AdjustedDate);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.Custom" /> dispatches to the registered handler and uses its activation result.
    /// </summary>
    [TestMethod]
    public void Apply_WhenCustomTrigger_ShouldUseHandlerRegistry()
    {
        var registry = new AdjustmentHandlerRegistry()
            .Register("shift-by-five", new ShiftByFiveDaysHandler());

        var adjuster = CreateAdjuster(handlers: registry);
        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.Custom,
            Action = AdjustmentAction.Custom,
            HandlerKey = "shift-by-five",
        };

        var result = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 6, 1));

        Assert.IsTrue(result.Activated);
        Assert.AreEqual(new DateTime(2025, 6, 6), result.AdjustedDate);
    }

    /// <summary>
    /// Verifies that scope evaluation excludes adjustments whose effective year window does not contain the active year.
    /// </summary>
    [TestMethod]
    public void IsInScope_WhenYearOutsideEffectiveWindow_ShouldReturnFalse()
    {
        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.None,
            EffectiveFromYear = 2030,
        };

        Assert.IsFalse(NotableDateAdjuster.IsInScope(adjustment, 2025, null, null));
        Assert.IsTrue(NotableDateAdjuster.IsInScope(adjustment, 2031, null, null));
    }

    /// <summary>
    /// Verifies that scope evaluation respects parent-territory containment when the rule scopes to a country and the query is for a
    /// subdivision.
    /// </summary>
    [TestMethod]
    public void IsInScope_WhenAdjustmentTerritoryParentOfRequested_ShouldReturnTrue()
    {
        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.None,
            TerritoryCode = "AU",
        };

        Assert.IsTrue(NotableDateAdjuster.IsInScope(adjustment, 2025, "AU-NSW", null));
    }

    /// <summary>
    /// Verifies that scope evaluation is bidirectional: an adjustment scoped to a subdivision (e.g. <c>AU-WA</c>) is also in scope
    /// when the call site is generating for the parent country (<c>AU</c>). This lets subdivision-specific substitutes fire while
    /// generating country-level rules; the generator tags the emitted occurrence with the narrower territory.
    /// </summary>
    [TestMethod]
    public void IsInScope_WhenAdjustmentTerritoryChildOfRequested_ShouldReturnTrue()
    {
        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.None,
            TerritoryCode = "AU-WA",
        };

        Assert.IsTrue(NotableDateAdjuster.IsInScope(adjustment, 2025, "AU", null));
    }

    /// <summary>
    /// Verifies that a peer-subdivision adjustment (e.g. <c>AU-WA</c>) does not activate when generating for a different,
    /// unrelated subdivision (<c>AU-NSW</c>).
    /// </summary>
    [TestMethod]
    public void IsInScope_WhenAdjustmentTerritoryIsUnrelatedSibling_ShouldReturnFalse()
    {
        var adjustment = new ObservanceAdjustment
        {
            Key = "test",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.None,
            TerritoryCode = "AU-WA",
        };

        Assert.IsFalse(NotableDateAdjuster.IsInScope(adjustment, 2025, "AU-NSW", null));
    }

    // -----------------------------------------------------------------------------------------
    // Argument-validation (null-argument) paths
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateAdjuster.Apply" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="adjustment" /> is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Apply_WhenAdjustmentIsNull_ShouldThrowExactly()
    {
        var adjuster = CreateAdjuster();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = adjuster.Apply(null!, SampleRule(), new DateTime(2025, 1, 1));
        });

        Assert.AreEqual("adjustment", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateAdjuster.Apply" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="rule" /> is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Apply_WhenRuleIsNull_ShouldThrowExactly()
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.None,
        };

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = adjuster.Apply(adjustment, null!, new DateTime(2025, 1, 1));
        });

        Assert.AreEqual("rule", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateAdjuster.IsInScope" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="adjustment" /> is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void IsInScope_WhenAdjustmentIsNull_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateAdjuster.IsInScope(null!, 2025, null, null);
        });

        Assert.AreEqual("adjustment", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the <see cref="NotableDateAdjuster" /> constructor throws when either
    /// required predicate is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIsWeekendIsNull_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new NotableDateAdjuster(
                isWeekend: null!,
                isNonWorkingDay: (d, t, c) => false,
                workingWeek: WeekPattern.Weekdays);
        });

        Assert.AreEqual("isWeekend", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the <see cref="NotableDateAdjuster" /> constructor throws when
    /// <paramref name="isNonWorkingDay" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIsNonWorkingDayIsNull_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new NotableDateAdjuster(
                isWeekend: _ => false,
                isNonWorkingDay: null!,
                workingWeek: WeekPattern.Weekdays);
        });

        Assert.AreEqual("isNonWorkingDay", ex.ParamName);
    }

    // -----------------------------------------------------------------------------------------
    // Custom-trigger / custom-action handler-lookup paths
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a <see cref="AdjustmentTrigger.Custom" /> adjustment is a no-op when the
    /// handler registry is absent, the handler key is missing, or the handler key is not
    /// registered — the adjuster returns a not-activated result with the original date in every
    /// such case.
    /// </summary>
    [DataRow(false, "missing-key")]
    [DataRow(true, null)]
    [DataRow(true, "")]
    [DataRow(true, "   ")]
    [DataRow(true, "not-registered")]
    [TestMethod]
    public void Apply_WhenCustomTriggerHasNoResolvableHandler_ShouldReturnNotActivated(
        bool useRegistry,
        string? handlerKey)
    {
        IAdjustmentHandlerRegistry? registry = useRegistry
            ? new AdjustmentHandlerRegistry().Register("other-key", new ShiftByFiveDaysHandler())
            : null;

        var adjuster = CreateAdjuster(handlers: registry);
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Custom,
            Action = AdjustmentAction.Custom,
            HandlerKey = handlerKey,
        };

        var original = new DateTime(2025, 6, 1);
        var result = adjuster.Apply(adjustment, SampleRule(), original);

        Assert.IsFalse(result.Activated);
        Assert.AreEqual(original, result.AdjustedDate);
    }

    /// <summary>
    /// Verifies that a custom handler that returns <c>Activated = false</c> produces a
    /// not-activated result from the adjuster even though the handler was dispatched.
    /// </summary>
    [TestMethod]
    public void Apply_WhenCustomTriggerAndHandlerDeclinesActivation_ShouldReturnNotActivated()
    {
        var registry = new AdjustmentHandlerRegistry().Register("decline", new DeclineHandler());
        var adjuster = CreateAdjuster(handlers: registry);
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Custom,
            Action = AdjustmentAction.Custom,
            HandlerKey = "decline",
        };

        var original = new DateTime(2025, 6, 1);
        var result = adjuster.Apply(adjustment, SampleRule(), original);

        Assert.IsFalse(result.Activated);
        Assert.AreEqual(original, result.AdjustedDate);
    }

    /// <summary>
    /// Verifies that a custom handler that returns a non-working override is surfaced on the
    /// <see cref="AdjustmentApplyResult" />.
    /// </summary>
    [TestMethod]
    public void Apply_WhenCustomHandlerReturnsNonWorkingOverride_ShouldForwardToResult()
    {
        var registry = new AdjustmentHandlerRegistry().Register(
            "flag-non-working",
            new FlagNonWorkingHandler());
        var adjuster = CreateAdjuster(handlers: registry);
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Custom,
            Action = AdjustmentAction.Custom,
            HandlerKey = "flag-non-working",
            IsNonWorkingDay = false,
        };

        var result = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 6, 1));

        Assert.IsTrue(result.Activated);
        Assert.IsTrue(result.IsNonWorkingOverride);
    }

    // -----------------------------------------------------------------------------------------
    // Action edge cases
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> returns the
    /// original date if no working day is found within 366 days (the bounded-walk fallback).
    /// </summary>
    [TestMethod]
    public void Apply_WhenMoveToNextNonWorkingDayPredicateNeverMatches_ShouldReturnOriginal()
    {
        // isNonWorking always returns true — every day is non-working — so no working day is ever found.
        var adjuster = CreateAdjuster(isNonWorking: (d, t, c) => true);
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.MoveToNextNonWorkingDay,
        };

        var original = new DateTime(2025, 6, 1);
        var result = adjuster.Apply(adjustment, SampleRule(), original);

        Assert.IsTrue(result.Activated);
        Assert.AreEqual(original, result.AdjustedDate);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.ReplaceWithNamedDate" /> falls back to the
    /// original date when either the target rule name is empty or no name-resolver callback is
    /// configured.
    /// </summary>
    [DataRow(null!)]
    [DataRow("")]
    [DataRow("   ")]
    [TestMethod]
    public void Apply_WhenReplaceWithNamedDateAndTargetIsMissing_ShouldReturnOriginal(string? target)
    {
        var adjuster = CreateAdjuster(resolveByName: (name, year, t, c) => new DateTime(year, 12, 26));
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.ReplaceWithNamedDate,
            TargetRuleName = target,
        };

        var original = new DateTime(2025, 6, 1);
        var result = adjuster.Apply(adjustment, SampleRule(), original);

        Assert.IsTrue(result.Activated);
        Assert.AreEqual(original, result.AdjustedDate);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.ReplaceWithNamedDate" /> returns the original
    /// date when no <c>resolveByName</c> callback is configured, regardless of whether the
    /// target name is present.
    /// </summary>
    [TestMethod]
    public void Apply_WhenReplaceWithNamedDateAndResolverIsNull_ShouldReturnOriginal()
    {
        var adjuster = CreateAdjuster(resolveByName: null);
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.ReplaceWithNamedDate,
            TargetRuleName = "Target",
        };

        var original = new DateTime(2025, 6, 1);
        var result = adjuster.Apply(adjustment, SampleRule(), original);

        Assert.IsTrue(result.Activated);
        Assert.AreEqual(original, result.AdjustedDate);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.ReplaceWithNamedDate" /> falls back to the
    /// original when the resolver returns <see langword="null" /> for the target rule.
    /// </summary>
    [TestMethod]
    public void Apply_WhenReplaceWithNamedDateAndResolverReturnsNull_ShouldReturnOriginal()
    {
        var adjuster = CreateAdjuster(resolveByName: (name, year, t, c) => null);
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.ReplaceWithNamedDate,
            TargetRuleName = "Target",
        };

        var original = new DateTime(2025, 6, 1);
        var result = adjuster.Apply(adjustment, SampleRule(), original);

        Assert.IsTrue(result.Activated);
        Assert.AreEqual(original, result.AdjustedDate);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.None" /> leaves the date unchanged even when
    /// the trigger activates.
    /// </summary>
    [TestMethod]
    public void Apply_WhenActionIsNone_ShouldActivateWithoutChangingDate()
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.None,
        };

        var original = new DateTime(2025, 6, 1);
        var result = adjuster.Apply(adjustment, SampleRule(), original);

        Assert.IsTrue(result.Activated);
        Assert.AreEqual(original, result.AdjustedDate);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.MoveToPreviousWeekday" /> walks backward from a
    /// Sunday onto the preceding Friday.
    /// </summary>
    [TestMethod]
    public void Apply_WhenSundayAndMoveToPreviousWeekday_ShouldYieldFriday()
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.MoveToPreviousWeekday,
        };

        var sunday = new DateTime(2026, 1, 4);
        var result = adjuster.Apply(adjustment, SampleRule(), sunday);

        Assert.IsTrue(result.Activated);
        Assert.AreEqual(DayOfWeek.Friday, result.AdjustedDate.DayOfWeek);
    }

    // -----------------------------------------------------------------------------------------
    // Trigger edge cases
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfWeekday" /> activates on weekdays and not on
    /// weekends.
    /// </summary>
    [DataRow("2025-06-02", true)]  // Monday
    [DataRow("2025-06-06", true)]  // Friday
    [DataRow("2025-06-07", false)] // Saturday
    [DataRow("2025-06-08", false)] // Sunday
    [TestMethod]
    public void Apply_IfWeekdayTrigger_ShouldActivateOnlyOnWeekdays(string dateIso, bool expectedActivation)
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.IfWeekday,
            Action = AdjustmentAction.None,
        };

        var result = adjuster.Apply(adjustment, SampleRule(), DateTime.Parse(dateIso));

        Assert.AreEqual(expectedActivation, result.Activated);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfAfterFixedDate" /> activates only when the
    /// original date sits strictly after the comparison date projected onto the active year.
    /// </summary>
    [DataRow("2030-05-10", false)]
    [DataRow("2030-06-01", false)]
    [DataRow("2030-06-02", true)]
    [DataRow("2030-11-01", true)]
    [TestMethod]
    public void Apply_IfAfterFixedDateTrigger_ShouldActivateOnStrictlyAfter(string originalIso, bool expectedActivation)
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.IfAfterFixedDate,
            Action = AdjustmentAction.AddDays,
            OffsetDays = 1,
            ComparisonDate = new DateTime(2000, 6, 1),
        };

        var result = adjuster.Apply(adjustment, SampleRule(), DateTime.Parse(originalIso));

        Assert.AreEqual(expectedActivation, result.Activated);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfDayOfWeek" /> returns false when the
    /// <see cref="ObservanceAdjustment.DayOfWeek" /> is <see langword="null" />, matching the
    /// production short-circuit.
    /// </summary>
    [TestMethod]
    public void Apply_WhenIfDayOfWeekAndDayOfWeekIsNull_ShouldNotActivate()
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.IfDayOfWeek,
            Action = AdjustmentAction.None,
            DayOfWeek = null,
        };

        var result = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 6, 2));

        Assert.IsFalse(result.Activated);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" /> does not activate
    /// when <see cref="ObservanceAdjustment.WeekOrdinal" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Apply_WhenIfNthOccurrenceAndOrdinalIsNull_ShouldNotActivate()
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.IfNthOccurrenceInMonth,
            Action = AdjustmentAction.None,
            WeekOrdinal = null,
        };

        var result = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 1, 8));

        Assert.IsFalse(result.Activated);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> and
    /// <see cref="AdjustmentTrigger.IfAfterFixedDate" /> short-circuit to not-activated when
    /// <see cref="ObservanceAdjustment.ComparisonDate" /> is <see langword="null" />.
    /// </summary>
    [DataRow(AdjustmentTrigger.IfBeforeFixedDate)]
    [DataRow(AdjustmentTrigger.IfAfterFixedDate)]
    [TestMethod]
    public void Apply_WhenFixedDateTriggerAndComparisonIsNull_ShouldNotActivate(AdjustmentTrigger trigger)
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = trigger,
            Action = AdjustmentAction.None,
            ComparisonDate = null,
        };

        var result = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 6, 1));

        Assert.IsFalse(result.Activated);
    }

    /// <summary>
    /// Verifies that a comparison date of 29 February is clamped to 28 February when projected
    /// onto a non-leap year, so <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> still
    /// evaluates cleanly without throwing.
    /// </summary>
    [TestMethod]
    public void Apply_WhenIfBeforeFixedDateAndComparisonIsFeb29InNonLeapYear_ShouldClampDayAndNotThrow()
    {
        var adjuster = CreateAdjuster();
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.IfBeforeFixedDate,
            Action = AdjustmentAction.AddDays,
            OffsetDays = 1,
            ComparisonDate = new DateTime(2024, 2, 29),
        };

        // Original in a non-leap year before 28 February ⇒ should activate; on/after 28 Feb ⇒ no.
        var before = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 2, 15));
        var onClamped = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 2, 28));
        var after = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 3, 1));

        Assert.IsTrue(before.Activated);
        Assert.IsFalse(onClamped.Activated);
        Assert.IsFalse(after.Activated);
    }

    // -----------------------------------------------------------------------------------------
    // Scope edge cases
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateAdjuster.IsInScope" /> returns false when the
    /// adjustment declares a <see cref="ObservanceAdjustment.CalendarType" /> that does not
    /// match the query-context calendar.
    /// </summary>
    [TestMethod]
    public void IsInScope_WhenAdjustmentCalendarDiffersFromContext_ShouldReturnFalse()
    {
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.None,
            CalendarType = typeof(System.Globalization.JulianCalendar),
        };

        Assert.IsFalse(NotableDateAdjuster.IsInScope(adjustment, 2025, null, typeof(System.Globalization.GregorianCalendar)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateAdjuster.IsInScope" /> returns false when the year is
    /// below <see cref="ObservanceAdjustment.EffectiveFromYear" /> OR above
    /// <see cref="ObservanceAdjustment.EffectiveToYear" />.
    /// </summary>
    [DataRow(2010, 2030, 2009, false)]
    [DataRow(2010, 2030, 2010, true)]
    [DataRow(2010, 2030, 2020, true)]
    [DataRow(2010, 2030, 2030, true)]
    [DataRow(2010, 2030, 2031, false)]
    [TestMethod]
    public void IsInScope_YearWindowTruthTable(int fromYear, int toYear, int queryYear, bool expected)
    {
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.None,
            EffectiveFromYear = fromYear,
            EffectiveToYear = toYear,
        };

        Assert.AreEqual(expected, NotableDateAdjuster.IsInScope(adjustment, queryYear, null, null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateAdjuster.IsInScope" /> returns false when the query
    /// territory cannot be parsed, even if the adjustment has a valid territory list.
    /// </summary>
    [TestMethod]
    public void IsInScope_WhenRequestedTerritoryIsMalformed_ShouldReturnFalse()
    {
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.None,
            TerritoryCode = "AU",
        };

        Assert.IsFalse(NotableDateAdjuster.IsInScope(adjustment, 2025, "BAD!", null));
    }

    private sealed class ShiftByFiveDaysHandler
        : IAdjustmentHandler
    {
        public AdjustmentHandlerResult Apply(AdjustmentHandlerContext context) =>
            new(true, context.Date.AddDays(5));
    }

    private sealed class DeclineHandler
        : IAdjustmentHandler
    {
        public AdjustmentHandlerResult Apply(AdjustmentHandlerContext context) =>
            new(false, context.Date);
    }

    private sealed class FlagNonWorkingHandler
        : IAdjustmentHandler
    {
        public AdjustmentHandlerResult Apply(AdjustmentHandlerContext context) =>
            new(true, context.Date, IsNonWorkingOverride: true);
    }
}
