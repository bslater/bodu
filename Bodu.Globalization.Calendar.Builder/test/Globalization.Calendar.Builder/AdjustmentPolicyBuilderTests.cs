// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentPolicyBuilderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Contains unit tests for <see cref="AdjustmentPolicyBuilder" /> — completeness validation and end-to-end emission.
/// </summary>
[TestClass]
public class AdjustmentPolicyBuilderTests
{
    /// <summary>
    /// Verifies that serializing a document whose adjustment policy is missing its required trigger, action, and
    /// emission throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenAdjustmentPolicyIncomplete_ShouldThrowInvalidOperationException()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("demo.policy")
            .AddAdjustmentPolicy("incomplete", a => a.WithPriority(1))
            .AddNotableDate("d", "D", NotableDateCategory.Observance, def => def.AddRule("default", r => r.Fixed(1, 1)));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => builder.ToXml());
    }

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

        Assert.IsTrue(anzac.IsObserved);
        Assert.AreEqual(new DateOnly(2027, 4, 26), anzac.Date);
    }
}
