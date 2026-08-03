// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentsAndPolicies.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Builder;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar.Samples.CustomCalendar.Scenarios;

/// <summary>
/// Demonstrates adjustment policies — the declarative weekend/in-lieu machinery — on an authored
/// calendar: a trigger (when does the policy fire), an action (what happens), and an emission mode
/// (what the query returns). The observed occurrence keeps its lineage to the actual date.
/// </summary>
public static class AdjustmentsAndPolicies
{
    /// <summary>
    /// Authors a holiday with a weekend-roll policy and resolves a year where it fires.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Adjustment policies: weekend substitution on an authored calendar ---");

        NotableDateResource resource = NotableDateDocumentBuilder.Create("contoso-observed")
            // The policy is declared once and referenced by any number of rules.
            .AddAdjustmentPolicy("weekend-roll", p => p
                .When(AdjustmentTrigger.IfWeekend)
                .Then(AdjustmentAction.MoveToNextWorkingDay)
                .Emit(EmissionMode.ObservedOnly)   // only the moved day is emitted; ActualAndObserved would keep both
                .WithReason("In-lieu day (weekend substitution)"))
            .AddNotableDate("founding-day", "Contoso Founding Day", NotableDateCategory.Other, c => c
                .AsNonWorkingByDefault()
                .AddRule("fixed", r => r
                    .Fixed(3, 12)
                    .WithAdjustment("weekend-roll")))
            .Build();

        var service = new NotableDateService(resource);

        // 2024: 12 March is a Tuesday - the policy does not fire.
        // 2022: 12 March is a Saturday - the observed day rolls to Monday 14 March.
        foreach (var year in new[] { 2024, 2022 })
        {
            NotableDate date = service.Resolve(year, "AU").Single();
            var detail = date.IsObserved
                ? $"observed (actual {date.ActualDate:yyyy-MM-dd}, {date.AdjustmentReason})"
                : "actual (no adjustment)";
            Console.WriteLine($"  {year}: {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek,-9}) {detail}");
        }

        Console.WriteLine();
    }
}
