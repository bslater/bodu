// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EuropeCalendarDataTests.GbBankHolidaySweep.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar;

public sealed partial class EuropeCalendarDataTests
{
    /// <summary>The per-territory services shared across the sweep rows, built once per home nation.</summary>
    private static readonly ConcurrentDictionary<string, INotableDateService> s_gbSweepServices = new();

    /// <summary>
    /// Verifies that every regular United Kingdom bank holiday resolves to the exact official GOV.UK date and
    /// substitute-day flag across the ten-year feed 2019-2028, per home nation.
    /// </summary>
    /// <param name="kat">The vector row carrying the (territory, year, bank holiday) input and the official outcome.</param>
    /// <remarks>
    /// <para>
    /// The sweep covers the rule-derived holidays only; the feed's sixteen royal and proclamation one-offs (jubilees,
    /// the state funeral, the coronation, the Scottish World Cup day, and the proclamation-moved 2020/2022 holidays)
    /// are outside the rule model and are excluded by the vector file, as its provenance header documents. Within the
    /// rule model the GB pack is expected to be exact — dates and substitute flags both — exercising the weekend
    /// roll, the Christmas/Boxing conflict-aware substitution, and the Scottish New Year chained substitution.
    /// </para>
    /// </remarks>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(GbBankHolidayVectors.Rows),
        typeof(GbBankHolidayVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Resolve_WhenSweptAcrossGovUkBankHolidayFeed_ShouldMatchOfficialDateAndObservedFlag(
        ValidKat<(string Territory, int Year, string ObservanceId), (DateOnly Date, bool IsObserved)> kat)
    {
        INotableDateService service = s_gbSweepServices.GetOrAdd(kat.Input.Territory, EuropeCalendarData.CreateService);

        var matches = service
            .Resolve(new DateRange(new DateOnly(kat.Input.Year, 1, 1), new DateOnly(kat.Input.Year, 12, 31)), kat.Input.Territory)
            .Where(r => r.NotableDateId == kat.Input.ObservanceId)
            .ToList();

        Assert.HasCount(1, matches, kat.Name);
        Assert.AreEqual(kat.Expected.Date, matches[0].Date, $"{kat.Name}: emitted date");
        Assert.AreEqual(kat.Expected.IsObserved, matches[0].IsObserved, $"{kat.Name}: observed flag");
    }
}
