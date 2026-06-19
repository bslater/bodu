// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentPolicyBuilderTests.AdjustmentPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public partial class AdjustmentPolicyBuilderTests
{
    /// <summary>
    /// Verifies that a complete adjustment policy referenced by a rule round-trips through the loader and applies its
    /// shift (a Sunday holiday observed on the following Monday).
    /// </summary>
    [TestMethod]
    public void AdjustmentPolicy_WhenComplete_ShouldApplyConfiguredShift()
    {
        NotableDateResource resource = NotableDateDocumentBuilder.Create("demo.policy")
            .AddAdjustmentPolicy("sunday-to-monday", a => a
                .When(AdjustmentTrigger.IfDayOfWeek)
                .OnTriggerWeekdays(DayOfWeek.Sunday)
                .Then(AdjustmentAction.MoveToNextWorkingDay)
                .Emit(RangeResolution.EmissionMode.ObservedOnly))
            .AddNotableDate("anzac-day", "Anzac Day", NotableDateCategory.PublicHoliday, d => d
                .AsNonWorkingByDefault()
                .AddRule("default", r => r.ForTerritory("AU").Fixed(4, 25).WithAdjustment("sunday-to-monday")))
            .Build();

        // 25 April 2027 is a Sunday; the policy observes it on Monday 26 April.
        NotableDate anzac = new NotableDateService(resource).Resolve(2027, "AU").Single();

        Assert.AreEqual(
            (new DateOnly(2027, 4, 26), true),
            (anzac.Date, anzac.IsObserved));
    }
}
