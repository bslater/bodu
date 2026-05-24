// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using SysGlob = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the derived-member semantics of the <see cref="NotableDate" /> record
/// (<see cref="NotableDate.EndDate" />, <see cref="NotableDate.WasAdjusted" />,
/// <see cref="NotableDate.DisplayName" />).
/// </summary>
[TestClass]
public sealed class NotableDateTests
{
    private static readonly DateTime Anchor = new DateTime(2026, 4, 1);

    /// <summary>
    /// Verifies that <see cref="NotableDate.EndDate" /> collapses onto the anchor when
    /// <see cref="NotableDate.DurationDays" /> is at or below one (including zero and negative
    /// values, which are defensively clamped).
    /// </summary>
    [DataRow(1)]
    [DataRow(0)]
    [DataRow(-5)]
    [TestMethod]
    public void EndDate_WhenDurationLessThanOrEqualOne_ShouldEqualAnchor(int duration)
    {
        NotableDate notable = Sample(duration: duration);

        Assert.AreEqual(Anchor, notable.EndDate);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDate.EndDate" /> advances by <c>DurationDays - 1</c> when
    /// the span is multi-day.
    /// </summary>
    [DataRow(2, "2026-04-02")]
    [DataRow(7, "2026-04-07")]
    [DataRow(31, "2026-05-01")]
    [TestMethod]
    public void EndDate_WhenMultiDaySpan_ShouldAdvanceByDurationMinusOne(int duration, string expected)
    {
        NotableDate notable = Sample(duration: duration);

        Assert.AreEqual(DateTime.Parse(expected), notable.EndDate);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDate.WasAdjusted" /> flips with the presence of an
    /// <see cref="NotableDate.AdjustmentReason" />.
    /// </summary>
    [TestMethod]
    public void WasAdjusted_ShouldMirrorAdjustmentReasonPresence()
    {
        NotableDate plain = Sample();
        NotableDate adjusted = plain with
        {
            AdjustmentReason = new AdjustmentReason(Anchor, AdjustmentTrigger.IfWeekend, AdjustmentAction.MoveToNextWeekday, null),
        };

        Assert.IsFalse(plain.WasAdjusted);
        Assert.IsTrue(adjusted.WasAdjusted);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDate.DisplayName" /> returns the bare name when neither a
    /// territory nor a calendar type is attached.
    /// </summary>
    [TestMethod]
    public void DisplayName_WhenNoTerritoryOrCalendar_ShouldReturnBareName()
    {
        NotableDate notable = Sample(name: "Test Day");

        Assert.AreEqual("Test Day", notable.DisplayName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDate.DisplayName" /> appends the territory when a
    /// territory is attached but no calendar type.
    /// </summary>
    [TestMethod]
    public void DisplayName_WhenTerritoryOnly_ShouldAppendTerritorySuffix()
    {
        NotableDate notable = Sample(name: "Labour Day") with { TerritoryCode = "AU-NSW" };

        Assert.AreEqual("Labour Day (AU-NSW)", notable.DisplayName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDate.DisplayName" /> appends a stripped calendar suffix
    /// when a calendar type is attached but no territory — the <c>Calendar</c> suffix is stripped.
    /// </summary>
    [TestMethod]
    public void DisplayName_WhenCalendarOnly_ShouldStripCalendarSuffix()
    {
        NotableDate notable = Sample(name: "Easter") with { CalendarType = typeof(SysGlob.JulianCalendar) };

        Assert.AreEqual("Easter (Julian)", notable.DisplayName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDate.DisplayName" /> concatenates territory and calendar
    /// suffixes when both are attached.
    /// </summary>
    [TestMethod]
    public void DisplayName_WhenTerritoryAndCalendarBothSet_ShouldIncludeBothSuffixes()
    {
        NotableDate notable = Sample(name: "Easter") with
        {
            TerritoryCode = "GR",
            CalendarType = typeof(SysGlob.JulianCalendar),
        };

        Assert.AreEqual("Easter (GR, Julian)", notable.DisplayName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDate.DurationDays" /> defaults to one when not supplied.
    /// </summary>
    [TestMethod]
    public void DurationDays_WhenUnset_ShouldDefaultToOne()
    {
        var notable = new NotableDate
        {
            Date = Anchor,
            Name = "X",
            Category = NotableDateCategory.None,
        };

        Assert.AreEqual(1, notable.DurationDays);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDate.Tags" /> defaults to an empty immutable set when not
    /// supplied.
    /// </summary>
    [TestMethod]
    public void Tags_WhenUnset_ShouldDefaultToEmpty()
    {
        var notable = new NotableDate
        {
            Date = Anchor,
            Name = "X",
            Category = NotableDateCategory.None,
        };

        Assert.IsNotNull(notable.Tags);
        Assert.AreEqual(0, notable.Tags.Count);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDate" /> record equality treats records with identical
    /// fields as equal, which underpins de-duplication in the collision resolver.
    /// </summary>
    [TestMethod]
    public void RecordEquality_WhenAllFieldsMatch_ShouldReturnTrue()
    {
        NotableDate left = Sample();
        NotableDate right = Sample();

        Assert.AreEqual(left, right);
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustment.HandlerParameters" /> is settable via the
    /// record <c>init</c> setter and round-trips through a record <c>with</c> expression.
    /// </summary>
    [TestMethod]
    public void ObservanceAdjustment_HandlerParameters_ShouldRoundTripThroughRecordWith()
    {
        var original = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Custom,
            Action = AdjustmentAction.Custom,
        };
        IReadOnlyDictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["alpha"] = "A",
            ["beta"] = "B",
        };

        ObservanceAdjustment updated = original with { HandlerParameters = parameters };

        Assert.IsNull(original.HandlerParameters);
        Assert.IsNotNull(updated.HandlerParameters);
        Assert.AreEqual("A", updated.HandlerParameters!["alpha"]);
        Assert.AreEqual("B", updated.HandlerParameters!["beta"]);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentReason" /> exposes every constructor-provided value
    /// via its auto-generated record members.
    /// </summary>
    [TestMethod]
    public void AdjustmentReason_ShouldExposeAllConstructorArguments()
    {
        var reason = new AdjustmentReason(
            originalDate: new DateTime(2025, 1, 1),
            trigger: AdjustmentTrigger.IfWeekend,
            action: AdjustmentAction.MoveToNextWeekday,
            handlerKey: "some-key");

        Assert.AreEqual(new DateTime(2025, 1, 1), reason.OriginalDate);
        Assert.AreEqual(AdjustmentTrigger.IfWeekend, reason.Trigger);
        Assert.AreEqual(AdjustmentAction.MoveToNextWeekday, reason.Action);
        Assert.AreEqual("some-key", reason.HandlerKey);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentHandlerContext" /> exposes every positional record
    /// parameter and participates in structural equality.
    /// </summary>
    [TestMethod]
    public void AdjustmentHandlerContext_ShouldExposeEveryPositionalParameter()
    {
        var adjustment = new ObservanceAdjustment
        {
            Key = "k",
            Trigger = AdjustmentTrigger.Always,
            Action = AdjustmentAction.None,
        };
        var rule = new NotableDateRule
        {
            Name = "Test",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
        };

        var ctx = new AdjustmentHandlerContext(
            Date: new DateTime(2025, 1, 1),
            Adjustment: adjustment,
            Rule: rule,
            TerritoryCode: "AU",
            CalendarType: typeof(System.Globalization.GregorianCalendar));
        var copy = new AdjustmentHandlerContext(
            Date: new DateTime(2025, 1, 1),
            Adjustment: adjustment,
            Rule: rule,
            TerritoryCode: "AU",
            CalendarType: typeof(System.Globalization.GregorianCalendar));

        Assert.AreEqual(new DateTime(2025, 1, 1), ctx.Date);
        Assert.AreSame(adjustment, ctx.Adjustment);
        Assert.AreSame(rule, ctx.Rule);
        Assert.AreEqual("AU", ctx.TerritoryCode);
        Assert.AreEqual(typeof(System.Globalization.GregorianCalendar), ctx.CalendarType);
        Assert.AreEqual(ctx, copy);
        Assert.AreEqual(ctx.GetHashCode(), copy.GetHashCode());
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentApplyResult.NotActivated(DateTime)" /> yields a result
    /// with the correct flag configuration for callers.
    /// </summary>
    [TestMethod]
    public void AdjustmentApplyResult_NotActivated_ShouldCarryFalseActivatedAndOriginalDate()
    {
        var result = AdjustmentApplyResult.NotActivated(new DateTime(2025, 3, 4));

        Assert.IsFalse(result.Activated);
        Assert.AreEqual(new DateTime(2025, 3, 4), result.AdjustedDate);
    }

    private static NotableDate Sample(string name = "Test Day", int duration = 1) =>
        new NotableDate
        {
            Date = Anchor,
            Name = name,
            Category = NotableDateCategory.Holiday,
            DurationDays = duration,
            Tags = ImmutableHashSet<string>.Empty,
        };
}
