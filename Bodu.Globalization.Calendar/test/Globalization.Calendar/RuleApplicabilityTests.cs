// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleApplicabilityTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the <see cref="RuleApplicability" /> truth tables: the year window (<c>fromYear</c>/<c>toYear</c>), the
/// explicit inclusion (<c>OnlyYear</c>) and exclusion (<c>ExceptYear</c>) sets, and the territory matching and
/// specificity rules, both directly and end-to-end through a filtered service resolve.
/// </summary>
/// <remarks>
/// <para>
/// The v1 applicability test exercised <c>FirstYear</c>/<c>LastYear</c> together with an <c>OccurrenceYears</c> modulo
/// cadence; v2 has no cadence and replaces it with explicit <see cref="RuleApplicability.OnlyYears" /> and
/// <see cref="RuleApplicability.ExceptYears" /> sets, so the cadence rows are not ported and the inclusion/exclusion
/// rows are added in their place.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class RuleApplicabilityTests
{
    /// <summary>
    /// Builds a <see cref="RuleApplicability" /> with the supplied year bounds and inclusion/exclusion sets for a global
    /// (territory-unscoped) rule.
    /// </summary>
    /// <param name="fromYear">The lower year bound, or <see langword="null" /> for none.</param>
    /// <param name="toYear">The upper year bound, or <see langword="null" /> for none.</param>
    /// <param name="onlyYears">The explicit inclusion years.</param>
    /// <param name="exceptYears">The explicit exclusion years.</param>
    /// <returns>The constructed applicability.</returns>
    private static RuleApplicability Years(int? fromYear, int? toYear, int[]? onlyYears = null, int[]? exceptYears = null) =>
        new(CalendarSystem.Gregorian, fromYear, toYear, [], onlyYears ?? [], exceptYears ?? []);

    /// <summary>
    /// Builds a <see cref="RuleApplicability" /> scoped to the supplied territories with no year restriction.
    /// </summary>
    /// <param name="territories">The territory codes the rule applies to.</param>
    /// <returns>The constructed applicability.</returns>
    private static RuleApplicability Territories(params string[] territories) =>
        new(CalendarSystem.Gregorian, null, null, territories, [], []);

    /// <summary>
    /// Builds a service over the applicability fixture.
    /// </summary>
    /// <returns>A service for the fixture.</returns>
    private static NotableDateService CreateService() =>
        NotableDateFixtures.Resolver("applicability.xml");

    /// <summary>
    /// Resolves the supplied territory and year and reports whether the supplied concept id is emitted.
    /// </summary>
    /// <param name="notableDateId">The concept id to look for.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year to resolve.</param>
    /// <returns><see langword="true" /> if the concept is emitted; otherwise <see langword="false" />.</returns>
    private static bool Emits(string notableDateId, string territory, int year)
    {
        DateRange range = new(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));

        return CreateService().Resolve(range, territory).Any(r => r.NotableDateId == notableDateId);
    }
}
