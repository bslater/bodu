// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScopedRemovalTests.PatchRuleExceptYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class ScopedRemovalTests
{
    /// <summary>
    /// Verifies that a PatchRule override excluding 2025 removes the 1 January occurrence for that year alone while
    /// leaving neighbouring years intact.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="expectedCount">The expected number of emitted occurrences on 1 January of that year.</param>
    [TestMethod]
    [DataRow(2024, 1)]  // neighbouring year intact
    [DataRow(2025, 0)]  // excluded year removed
    [DataRow(2026, 1)]  // neighbouring year intact
    public void PatchRuleExceptYear_WhenYearExcluded_ShouldRemoveOccurrenceForThatYearOnly(int year, int expectedCount)
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(YearScopedXml));

        Assert.AreEqual(expectedCount, Count(service, new DateOnly(year, 1, 1), "XX"));
    }
}
