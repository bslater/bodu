// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.AdjustmentScope.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that a territory-scoped adjustment does not leak onto a global (territory-neutral) rule unless explicitly
/// opted in (F-012).
/// </summary>
public sealed partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that a territory-scoped adjustment on a global rule does not activate by default, so the actual date is
    /// emitted unadjusted.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenTerritoryScopedAdjustmentOnGlobalRule_ShouldNotApplyByDefault()
    {
        // 1 January 2022 is a Saturday. The adjustment is scoped to AU-NSW, but the rule is global (no territory).
        NotableDateRule rule = Fixed("Global Holiday", 1, 1) with
        {
            IsNonWorkingDay = true,
            Adjustments =
            [
                new ObservanceAdjustment { Key = "nsw-sub", Trigger = AdjustmentTrigger.IfWeekend, Action = AdjustmentAction.AddDays, OffsetDays = 2, TerritoryCode = "AU-NSW" },
            ],
        };
        NotableDateService service = BuildService(rule);

        var results = service.GetNotableDates(2022).Where(d => d.Name == "Global Holiday").ToList();

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(new DateTime(2022, 1, 1), results[0].Date);
        Assert.IsFalse(results[0].WasAdjusted, "A territory-scoped adjustment must not apply to a global rule by default.");
    }

    /// <summary>
    /// Verifies that setting <see cref="ObservanceAdjustment.AppliesToGlobalRules" /> lets a territory-scoped adjustment
    /// activate on a global rule.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenAppliesToGlobalRulesSet_ShouldApplyToGlobalRule()
    {
        NotableDateRule rule = Fixed("Global Opt", 1, 1) with
        {
            IsNonWorkingDay = true,
            Adjustments =
            [
                new ObservanceAdjustment { Key = "nsw-sub", Trigger = AdjustmentTrigger.IfWeekend, Action = AdjustmentAction.AddDays, OffsetDays = 2, TerritoryCode = "AU-NSW", AppliesToGlobalRules = true },
            ],
        };
        NotableDateService service = BuildService(rule);

        NotableDate observed = service.GetNotableDates(2022).Single(d => d.Name == "Global Opt" && d.WasAdjusted);

        Assert.AreEqual(new DateTime(2022, 1, 3), observed.Date);
    }
}
