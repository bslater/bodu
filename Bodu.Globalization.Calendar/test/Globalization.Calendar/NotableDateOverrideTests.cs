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
    /// Verifies that removing the Puerto Rico Constitution Day rule leaves the United States rule intact. (T12)
    /// </summary>
    [TestMethod]
    public void Override_RemoveRule_WhenTargetingPuertoRicoConstitutionDay_DoesNotRemoveUnitedStatesRule()
    {
        NotableDateService resolver = new(MinimalNotableDates.LoadWithRemoveOverride());

        IReadOnlyList<NotableDate> unitedStates = resolver.Resolve(new DateOnly(2026, 9, 17), "US");
        Assert.AreEqual(1, unitedStates.Count, "United States rule should survive the removal");
        Assert.AreEqual("us-fixed-sep-17", unitedStates[0].RuleId);

        IReadOnlyList<NotableDate> puertoRico = resolver.Resolve(new DateOnly(2026, 7, 25), "PR");
        Assert.AreEqual(0, puertoRico.Count, "Puerto Rico rule should have been removed");
    }

    /// <summary>
    /// Verifies that patching the Puerto Rico Constitution Day rule's category does not change the sibling United States
    /// rule's category. (T13)
    /// </summary>
    [TestMethod]
    public void Override_PatchRule_WhenTargetingPuertoRicoConstitutionDay_DoesNotPatchUnitedStatesRule()
    {
        NotableDateService resolver = new(MinimalNotableDates.LoadWithPatchOverride());

        IReadOnlyList<NotableDate> puertoRico = resolver.Resolve(new DateOnly(2026, 7, 25), "PR");
        Assert.AreEqual(1, puertoRico.Count);
        Assert.AreEqual(NotableDateCategory.PublicHoliday, puertoRico[0].Category, "patched Puerto Rico category");

        IReadOnlyList<NotableDate> unitedStates = resolver.Resolve(new DateOnly(2026, 9, 17), "US");
        Assert.AreEqual(1, unitedStates.Count);
        Assert.AreEqual(NotableDateCategory.Observance, unitedStates[0].Category, "unchanged United States category");
    }
}
