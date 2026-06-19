// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the adjustment matrix end to end: every emission mode, day-shifting action, and trigger type, including
/// cross-year observed shifts. The fixture anchors each example on 1 January, whose weekday varies by year (2022 is a
/// Saturday, 2023 a Sunday, 2026 a Thursday).
/// </summary>
[TestClass]
public sealed partial class AdjustmentTests
{
    private const string Territory = "ZZ";

    /// <summary>
    /// Builds a resolver over the adjustments fixture.
    /// </summary>
    /// <returns>A resolver for the adjustments fixture.</returns>
    private static NotableDateService CreateResolver() =>
        NotableDateFixtures.Resolver("adjustments.xml");

    /// <summary>
    /// Returns the single resolved occurrence with the supplied notable-date id, asserting exactly one match.
    /// </summary>
    /// <param name="results">The resolver results.</param>
    /// <param name="notableDateId">The notable-date id to select.</param>
    /// <returns>The matching occurrence.</returns>
    private static NotableDate Single(IReadOnlyList<NotableDate> results, string notableDateId)
    {
        var matches = results.Where(r => r.NotableDateId == notableDateId).ToList();
        Assert.HasCount(1, matches, $"expected exactly one '{notableDateId}'");
        return matches[0];
    }

    /// <summary>
    /// Counts the resolved occurrences with the supplied notable-date id.
    /// </summary>
    /// <param name="results">The resolver results.</param>
    /// <param name="notableDateId">The notable-date id to count.</param>
    /// <returns>The number of matching occurrences.</returns>
    private static int Count(IReadOnlyList<NotableDate> results, string notableDateId) =>
        results.Count(r => r.NotableDateId == notableDateId);
}
