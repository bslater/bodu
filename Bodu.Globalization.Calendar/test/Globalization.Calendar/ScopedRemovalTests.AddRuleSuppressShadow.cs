// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScopedRemovalTests.AddRuleSuppressShadow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class ScopedRemovalTests
{
    /// <summary>
    /// Verifies that an AddRule override injecting a more-specific suppressing rule removes the 26 January 2025
    /// occurrence for the shadowed subterritory alone while leaving the parent and sibling subterritories intact.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="expectedCount">The expected number of emitted occurrences for that territory.</param>
    [TestMethod]
    [DataRow("AU", 1)]       // national parent intact
    [DataRow("AU-NSW", 0)]   // shadowed subterritory removed
    [DataRow("AU-VIC", 1)]   // sibling subterritory intact
    public void AddRuleSuppressShadow_WhenSubterritoryShadowed_ShouldRemoveOccurrenceForThatTerritoryOnly(string territory, int expectedCount)
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(TerritoryScopedXml));

        Assert.AreEqual(expectedCount, Count(service, new DateOnly(2025, 1, 26), territory));
    }
}
