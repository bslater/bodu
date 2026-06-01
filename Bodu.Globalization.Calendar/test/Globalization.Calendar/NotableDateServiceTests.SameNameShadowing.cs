// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.SameNameShadowing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Pins the same-name territory-shadowing contract applied by <c>NotableDateRangePlanner</c>. The pattern lets a data
/// pack author a canonical national rule together with per-subdivision variants that override it; the planner
/// suppresses the broader rule for queries that match a narrower same-name rule.
/// </summary>
public partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that a query for the request territory itself prefers the narrower same-name rule (NSW) and suppresses
    /// the canonical AU rule. The NSW rule's adjustment fires on Saturday Apr 25 2026 and emits the substitute Monday.
    /// </summary>
    [TestMethod]
    public void Shadowing_WhenNarrowerSameNameRuleMatchesRequest_ShouldSuppressBroader_ForAuNsw()
    {
        NotableDateRule auBase = Fixed("Anzac Day", 4, 25, nonWorking: true, territory: "AU");
        NotableDateRule nswOverride = Fixed("Anzac Day", 4, 25, nonWorking: true, territory: "AU-NSW") with
        {
            Adjustments = [new ObservanceAdjustment
            {
                Key = "nsw-weekend-sub",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextWeekday,
            }],
        };

        NotableDateService service = BuildService(auBase, nswOverride);

        IReadOnlyList<NotableDate> nsw = service.GetNotableDates(2026, "AU-NSW")
            .Where(d => d.Name == "Anzac Day")
            .ToList();

        Assert.AreEqual(1, nsw.Count, "Narrower AU-NSW rule must shadow the AU-wide rule for AU-NSW queries.");
        Assert.AreEqual(new DateTime(2026, 4, 27), nsw[0].Date);
        Assert.IsTrue(nsw[0].WasAdjusted);
    }

    /// <summary>
    /// Verifies that a query for a sibling subdivision (with no same-name rule of its own) falls back to the canonical
    /// broader AU rule. The AU rule carries no adjustment, so the observance surfaces on Apr 25 unchanged.
    /// </summary>
    [TestMethod]
    public void Shadowing_WhenNoNarrowerSameNameRuleMatchesRequest_ShouldUseBroader_ForAuVic()
    {
        NotableDateRule auBase = Fixed("Anzac Day", 4, 25, nonWorking: true, territory: "AU");
        NotableDateRule nswOverride = Fixed("Anzac Day", 4, 25, nonWorking: true, territory: "AU-NSW") with
        {
            Adjustments = [new ObservanceAdjustment
            {
                Key = "nsw-weekend-sub",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextWeekday,
            }],
        };

        NotableDateService service = BuildService(auBase, nswOverride);

        IReadOnlyList<NotableDate> vic = service.GetNotableDates(2026, "AU-VIC")
            .Where(d => d.Name == "Anzac Day")
            .ToList();

        Assert.AreEqual(1, vic.Count, "AU-VIC has no narrower rule; the AU canonical rule applies.");
        Assert.AreEqual(new DateTime(2026, 4, 25), vic[0].Date);
        Assert.IsFalse(vic[0].WasAdjusted);
    }

    /// <summary>
    /// Verifies that a parent-territory query keeps both the canonical broader rule and every narrower same-name
    /// variant. The user asking for "all of AU" expects per-state refinements to surface alongside the canonical
    /// observance.
    /// </summary>
    [TestMethod]
    public void Shadowing_WhenRequestIsParentTerritory_ShouldKeepBothBroaderAndNarrowerRules()
    {
        NotableDateRule auBase = Fixed("Anzac Day", 4, 25, nonWorking: true, territory: "AU");
        NotableDateRule nswOverride = Fixed("Anzac Day", 4, 25, nonWorking: true, territory: "AU-NSW") with
        {
            Adjustments = [new ObservanceAdjustment
            {
                Key = "nsw-weekend-sub",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextWeekday,
            }],
        };

        NotableDateService service = BuildService(auBase, nswOverride);

        IReadOnlyList<NotableDate> au = service.GetNotableDates(2026, "AU")
            .Where(d => d.Name == "Anzac Day")
            .OrderBy(d => d.TerritoryCode, StringComparer.Ordinal)
            .ToList();

        Assert.AreEqual(2, au.Count, "Parent-territory query keeps both the canonical and the per-subdivision variants.");
        Assert.AreEqual("AU", au[0].TerritoryCode);
        Assert.AreEqual(new DateTime(2026, 4, 25), au[0].Date);
        Assert.AreEqual("AU-NSW", au[1].TerritoryCode);
        Assert.AreEqual(new DateTime(2026, 4, 27), au[1].Date);
    }

    /// <summary>
    /// Verifies that NSW's year-scoped adjustment (e.g. weekend substitute only for 2026–2028) falls back to the
    /// canonical AU rule outside that window. Sketched scenario: NSW rule carries an adjustment that only fires for a
    /// bounded year range. In a year outside that range, the NSW rule still wins via shadowing but its adjustment does
    /// not activate, so the observance surfaces on its base date — equivalent to the AU rule's emission.
    /// </summary>
    [TestMethod]
    public void Shadowing_WhenNarrowerRuleAdjustmentIsYearScoped_ShouldStillShadowOutsideAdjustmentWindow()
    {
        NotableDateRule auBase = Fixed("Anzac Day", 4, 25, nonWorking: true, territory: "AU");
        NotableDateRule nswOverride = Fixed("Anzac Day", 4, 25, nonWorking: true, territory: "AU-NSW") with
        {
            Adjustments = [new ObservanceAdjustment
            {
                Key = "nsw-weekend-sub-2026-2028",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextWeekday,
                EffectiveFromYear = 2026,
                EffectiveToYear = 2028,
            }],
        };

        NotableDateService service = BuildService(auBase, nswOverride);

        // 25 April 2020 is a Saturday but the NSW adjustment window has not opened yet.
        IReadOnlyList<NotableDate> nsw2020 = service.GetNotableDates(2020, "AU-NSW")
            .Where(d => d.Name == "Anzac Day")
            .ToList();
        Assert.AreEqual(1, nsw2020.Count);
        Assert.AreEqual(new DateTime(2020, 4, 25), nsw2020[0].Date, "NSW rule still wins shadowing; its adjustment is dormant outside 2026-2028 so the base date emits.");
        Assert.IsFalse(nsw2020[0].WasAdjusted);

        // 25 April 2026 is a Saturday inside the NSW adjustment window — the substitute fires.
        IReadOnlyList<NotableDate> nsw2026 = service.GetNotableDates(2026, "AU-NSW")
            .Where(d => d.Name == "Anzac Day")
            .ToList();
        Assert.AreEqual(1, nsw2026.Count);
        Assert.AreEqual(new DateTime(2026, 4, 27), nsw2026[0].Date);
        Assert.IsTrue(nsw2026[0].WasAdjusted);
    }

    /// <summary>
    /// Verifies that a rule with no authored territory is treated as global and is therefore shadowed by any narrower
    /// same-name rule that matches the request.
    /// </summary>
    [TestMethod]
    public void Shadowing_WhenBroaderRuleHasNoTerritory_ShouldStillBeSuppressedByNarrowerMatch()
    {
        NotableDateRule global = Fixed("Anzac Day", 4, 25, nonWorking: true);
        NotableDateRule nswOverride = Fixed("Anzac Day", 4, 25, nonWorking: true, territory: "AU-NSW");

        NotableDateService service = BuildService(global, nswOverride);

        IReadOnlyList<NotableDate> nsw = service.GetNotableDates(2026, "AU-NSW")
            .Where(d => d.Name == "Anzac Day")
            .ToList();

        Assert.AreEqual(1, nsw.Count);
        Assert.AreEqual("AU-NSW", nsw[0].TerritoryCode, "Narrower territory-scoped rule wins over the global-default rule.");
    }

    /// <summary>
    /// Verifies that an any-territory query (no request territory supplied) returns every same-name rule. Shadowing
    /// only fires when the request itself carries a specific territory; "give me everything" answers do not collapse
    /// per-subdivision variants into the canonical entry.
    /// </summary>
    [TestMethod]
    public void Shadowing_WhenRequestTerritoryIsNull_ShouldNotShadow()
    {
        NotableDateRule auBase = Fixed("Anzac Day", 4, 25, nonWorking: true, territory: "AU");
        NotableDateRule nswOverride = Fixed("Anzac Day", 4, 25, nonWorking: true, territory: "AU-NSW") with
        {
            Adjustments = [new ObservanceAdjustment
            {
                Key = "nsw-weekend-sub",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextWeekday,
            }],
        };

        NotableDateService service = BuildService(auBase, nswOverride);

        IReadOnlyList<NotableDate> any = service.GetNotableDates(2026)
            .Where(d => d.Name == "Anzac Day")
            .OrderBy(d => d.TerritoryCode, StringComparer.Ordinal)
            .ToList();

        Assert.AreEqual(2, any.Count, "Any-territory query preserves every variant.");
    }
}
