// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryNotableDateAssertions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Provides the shared assertion routines and <c>DynamicDataDisplayName</c> helper that every
/// <see cref="TerritoryNotableDateKnownAnswer" /> and <see cref="RuleCatalogueExpectation" /> driven test consumes
/// across the three calendar Data test projects.
/// </summary>
public static class TerritoryNotableDateAssertions
{
    /// <summary>
    /// Constructs a <see cref="NotableDateService" /> from the row's
    /// <see cref="TerritoryNotableDateKnownAnswer.ProviderFactory" />, queries <c>GetNotableDates(Year, Territory)</c>,
    /// filters by <see cref="TerritoryNotableDateKnownAnswer.Name" />, and asserts the expected occurrence (or absence)
    /// per the row's flags.
    /// </summary>
    /// <param name="ka">The known-answer row to verify.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="ka" /> is <see langword="null" />.
    /// </exception>
    public static void AssertExpectedOccurrence(TerritoryNotableDateKnownAnswer ka)
    {
        ArgumentNullException.ThrowIfNull(ka);

        NotableDateService service = new(
            [ka.ProviderFactory()],
            WorkingDaysOfWeek.MondayToFriday);

        string expectedTerritory = ka.ExpectedTerritoryCode ?? ka.Territory;

        // Filter by name AND expected carrying territory: a country-level query surfaces subdivision
        // shadows alongside the canonical rule, so the row identifies which one is being asserted.
        List<NotableDate> matches = service.GetNotableDates(ka.Year, ka.Territory)
            .Where(d => d.Name == ka.Name && d.TerritoryCode == expectedTerritory)
            .OrderBy(d => d.Date)
            .ToList();

        string label = $"{ka.Territory} {ka.Year} '{ka.Name}' [{expectedTerritory}]";

        if (!ka.ShouldExist)
        {
            Assert.AreEqual(
                0,
                matches.Count,
                $"{label}: expected no occurrence; got {matches.Count}.");
            return;
        }

        Assert.AreEqual(
            1,
            matches.Count,
            $"{label}: expected exactly one occurrence; got {matches.Count}.");

        NotableDate occurrence = matches[0];

        Assert.AreEqual(ka.ExpectedDate, occurrence.Date, $"{label}: Date mismatch.");

        if (ka.ExpectedDayOfWeek is DayOfWeek expectedDow)
        {
            Assert.AreEqual(expectedDow, occurrence.Date.DayOfWeek, $"{label}: DayOfWeek mismatch.");
        }

        Assert.AreEqual(ka.WasAdjusted, occurrence.WasAdjusted, $"{label}: WasAdjusted mismatch.");

        if (ka.OriginalDate is DateTime expectedOriginal)
        {
            Assert.IsNotNull(occurrence.AdjustmentReason, $"{label}: expected AdjustmentReason.");
            Assert.AreEqual(
                expectedOriginal,
                occurrence.AdjustmentReason!.OriginalDate,
                $"{label}: AdjustmentReason.OriginalDate mismatch.");
        }
    }

    /// <summary>
    /// Invokes the row's <see cref="RuleCatalogueExpectation.ProviderFactory" />, materialises the flattened rule-name
    /// set via <c>LoadRules()</c>, and asserts that every entry in <see cref="RuleCatalogueExpectation.Includes" /> is
    /// present and every entry in <see cref="RuleCatalogueExpectation.Excludes" /> is absent.
    /// </summary>
    /// <param name="expectation">The catalogue expectation to verify.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="expectation" /> is <see langword="null" />.
    /// </exception>
    public static void AssertRuleCatalogue(RuleCatalogueExpectation expectation)
    {
        ArgumentNullException.ThrowIfNull(expectation);

        HashSet<string> names = expectation.ProviderFactory().LoadRules()
            .Select(r => r.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string include in expectation.Includes)
        {
            Assert.IsTrue(
                names.Contains(include),
                $"{expectation.Territory} catalogue: expected rule '{include}' to be present.");
        }

        foreach (string exclude in expectation.Excludes)
        {
            Assert.IsFalse(
                names.Contains(exclude),
                $"{expectation.Territory} catalogue: expected rule '{exclude}' to be absent.");
        }
    }

    /// <summary>
    /// Renders a <see cref="TerritoryNotableDateKnownAnswer" /> or <see cref="RuleCatalogueExpectation" /> row as the
    /// MSTest display name. Occurrence rows render as
    /// <c>"AU-NSW | 2026 | Anzac Day -&gt; 2026-04-27 [adjusted from 2026-04-25]"</c>; catalogue rows render as
    /// <c>"US | includes 8, excludes 4"</c>.
    /// </summary>
    /// <param name="methodInfo">
    /// The test method under discovery. Unused; required by
    /// <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" />.
    /// </param>
    /// <param name="data">The single-element row produced by one of the provider methods.</param>
    /// <returns>An ASCII display name no longer than ~100 characters.</returns>
    public static string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        _ = methodInfo;

        return data[0] switch
        {
            TerritoryNotableDateKnownAnswer ka => FormatOccurrence(ka),
            RuleCatalogueExpectation re =>
                $"{re.Territory} | includes {re.Includes.Count}, excludes {re.Excludes.Count}",
            _ => string.Join(", ", data),
        };
    }

    /// <summary>
    /// Formats a <see cref="TerritoryNotableDateKnownAnswer" /> row as a pipe-separated, ASCII-only display name.
    /// Suffixes "[absent]" when the row encodes a non-existence assertion, "[adjusted from ...]" when the row encodes a
    /// weekend substitute, and the note when populated.
    /// </summary>
    /// <param name="ka">The row to format.</param>
    /// <returns>The rendered display name.</returns>
    private static string FormatOccurrence(TerritoryNotableDateKnownAnswer ka)
    {
        string result = ka.ShouldExist
            ? $"{ka.Territory} | {ka.Year} | {ka.Name} -> {ka.ExpectedDate:yyyy-MM-dd}"
            : $"{ka.Territory} | {ka.Year} | {ka.Name} [absent]";

        if (ka.WasAdjusted && ka.OriginalDate is DateTime originalDate)
        {
            result += $" [adjusted from {originalDate:yyyy-MM-dd}]";
        }

        if (!string.IsNullOrEmpty(ka.Note))
        {
            result += $" [{ka.Note}]";
        }

        return result;
    }
}
