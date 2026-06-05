// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.AdjustmentTerritory.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that an adjustment's emitted territory is normalized to a single valid territory rather than a raw or
/// comma-separated string (F-013).
/// </summary>
public sealed partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that a single-territory adjustment re-tags the observed occurrence with the normalized (upper-case)
    /// territory code.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenAdjustmentRetagsSingleTerritory_ShouldNormalizeTerritory()
    {
        // 1 January 2022 is a Saturday, so the weekend roll fires and re-tags the substitute to the subdivision.
        NotableDateRule rule = Fixed("Substitute Test", 1, 1, territory: "AU") with
        {
            IsNonWorkingDay = true,
            Adjustments =
            [
                new ObservanceAdjustment { Key = "wa-sub", Trigger = AdjustmentTrigger.IfWeekend, Action = AdjustmentAction.AddDays, OffsetDays = 2, TerritoryCode = "au-nsw" },
            ],
        };
        NotableDateService service = BuildService(rule);

        NotableDate observed = service.GetNotableDates(2022, territoryCode: "AU")
            .Single(d => d.Name == "Substitute Test" && d.WasAdjusted);

        Assert.AreEqual("AU-NSW", observed.TerritoryCode);
    }

    /// <summary>
    /// Verifies that a comma-separated adjustment territory is never emitted verbatim as a single territory code.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenAdjustmentTerritoryIsCommaList_ShouldNotEmitCommaSeparatedTerritory()
    {
        NotableDateRule rule = Fixed("Multi Sub", 1, 1, territory: "AU") with
        {
            IsNonWorkingDay = true,
            Adjustments =
            [
                new ObservanceAdjustment { Key = "multi", Trigger = AdjustmentTrigger.IfWeekend, Action = AdjustmentAction.AddDays, OffsetDays = 2, TerritoryCode = "AU-NSW,AU-VIC" },
            ],
        };
        NotableDateService service = BuildService(rule);

        NotableDate observed = service.GetNotableDates(2022, territoryCode: "AU")
            .Single(d => d.Name == "Multi Sub" && d.WasAdjusted);

        Assert.IsFalse((observed.TerritoryCode ?? string.Empty).Contains(','), "A comma-separated territory must not be emitted as a single territory code.");
    }
}
