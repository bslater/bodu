// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOverrideTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that ID-targeted override operations affect exactly one rule identity and never its sibling rules under the
/// same notable-date concept.
/// </summary>
[TestClass]
public sealed class NotableDateOverrideTests
{
    /// <summary>
    /// Verifies that removing the Puerto Rico Constitution Day rule leaves the sibling United States rule intact. (T12)
    /// </summary>
    [TestMethod]
    public void Override_RemoveRule_WhenTargetingPuertoRicoConstitutionDay_ShouldKeepUnitedStatesRule()
    {
        NotableDateService resolver = new(MinimalNotableDates.LoadWithRemoveOverride());

        IReadOnlyList<NotableDate> unitedStates = resolver.Resolve(new DateOnly(2026, 9, 17), "US");

        CollectionAssert.AreEqual(
            new[] { "us-fixed-sep-17" },
            unitedStates.Select(r => r.RuleId).ToArray());
    }

    /// <summary>
    /// Verifies that the remove override removes the targeted Puerto Rico Constitution Day rule. (T12)
    /// </summary>
    [TestMethod]
    public void Override_RemoveRule_WhenTargetingPuertoRicoConstitutionDay_ShouldRemovePuertoRicoRule()
    {
        NotableDateService resolver = new(MinimalNotableDates.LoadWithRemoveOverride());

        IReadOnlyList<NotableDate> puertoRico = resolver.Resolve(new DateOnly(2026, 7, 25), "PR");

        Assert.IsEmpty(puertoRico);
    }

    /// <summary>
    /// Verifies that patching the Puerto Rico Constitution Day rule's category applies the patched category to the
    /// Puerto Rico rule. (T13)
    /// </summary>
    [TestMethod]
    public void Override_PatchRule_WhenTargetingPuertoRicoConstitutionDay_ShouldPatchPuertoRicoCategory()
    {
        NotableDateService resolver = new(MinimalNotableDates.LoadWithPatchOverride());

        IReadOnlyList<NotableDate> puertoRico = resolver.Resolve(new DateOnly(2026, 7, 25), "PR");

        CollectionAssert.AreEqual(
            new[] { NotableDateCategory.PublicHoliday },
            puertoRico.Select(r => r.Category).ToArray());
    }

    /// <summary>
    /// Verifies that patching the Puerto Rico Constitution Day rule's category does not change the sibling United States
    /// rule's category. (T13)
    /// </summary>
    [TestMethod]
    public void Override_PatchRule_WhenTargetingPuertoRicoConstitutionDay_ShouldLeaveUnitedStatesCategoryUnchanged()
    {
        NotableDateService resolver = new(MinimalNotableDates.LoadWithPatchOverride());

        IReadOnlyList<NotableDate> unitedStates = resolver.Resolve(new DateOnly(2026, 9, 17), "US");

        CollectionAssert.AreEqual(
            new[] { NotableDateCategory.Observance },
            unitedStates.Select(r => r.Category).ToArray());
    }
}
