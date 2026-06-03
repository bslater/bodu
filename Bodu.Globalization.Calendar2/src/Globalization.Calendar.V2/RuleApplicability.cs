// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleApplicability.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Describes where and when a rule applies: its calendar system, optional year bounds, explicit territories, and
/// optional inclusion/exclusion years.
/// </summary>
/// <remarks>
/// <para>
/// Territories are explicit codes; there are no comma-delimited strings. A rule with no territories is treated as
/// global and applies to every requested territory. A subnational query (for example <c>AU-NSW</c>) matches a rule
/// scoped to its parent territory (<c>AU</c>), but a national query does not match a subnational-only rule.
/// </para>
/// </remarks>
public sealed class RuleApplicability
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuleApplicability" /> class.
    /// </summary>
    /// <param name="calendar">The calendar system the rule's strategy is expressed in.</param>
    /// <param name="fromYear">
    /// The first applicable Gregorian year, or <see langword="null" /> for no lower bound.
    /// </param>
    /// <param name="toYear">The last applicable Gregorian year, or <see langword="null" /> for no upper bound.</param>
    /// <param name="territories">The explicit territories the rule applies to.</param>
    /// <param name="onlyYears">The explicit inclusion years, or an empty sequence for no restriction.</param>
    /// <param name="exceptYears">The explicit exclusion years, or an empty sequence for no exclusions.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="territories" />, <paramref name="onlyYears" />, or <paramref name="exceptYears" /> is
    /// <see langword="null" />.
    /// </exception>
    public RuleApplicability(
        CalendarSystem calendar,
        int? fromYear,
        int? toYear,
        IEnumerable<string> territories,
        IEnumerable<int> onlyYears,
        IEnumerable<int> exceptYears)
    {
        ThrowHelper.ThrowIfNull(territories);
        ThrowHelper.ThrowIfNull(onlyYears);
        ThrowHelper.ThrowIfNull(exceptYears);

        this.Calendar = calendar;
        this.FromYear = fromYear;
        this.ToYear = toYear;
        this.Territories = territories.ToArray();
        this.OnlyYears = onlyYears.ToArray();
        this.ExceptYears = exceptYears.ToArray();
    }

    /// <summary>
    /// Gets the calendar system the rule's strategy is expressed in.
    /// </summary>
    /// <returns>The configured <see cref="CalendarSystem" />.</returns>
    public CalendarSystem Calendar { get; }

    /// <summary>
    /// Gets the first applicable Gregorian year.
    /// </summary>
    /// <returns>The lower year bound, or <see langword="null" /> when unbounded.</returns>
    public int? FromYear { get; }

    /// <summary>
    /// Gets the last applicable Gregorian year.
    /// </summary>
    /// <returns>The upper year bound, or <see langword="null" /> when unbounded.</returns>
    public int? ToYear { get; }

    /// <summary>
    /// Gets the explicit territories the rule applies to.
    /// </summary>
    /// <returns>The territory codes; empty when the rule is global.</returns>
    public IReadOnlyList<string> Territories { get; }

    /// <summary>
    /// Gets the explicit inclusion years.
    /// </summary>
    /// <returns>The years the rule is restricted to; empty when unrestricted.</returns>
    public IReadOnlyList<int> OnlyYears { get; }

    /// <summary>
    /// Gets the explicit exclusion years.
    /// </summary>
    /// <returns>The years the rule is suppressed for; empty when there are none.</returns>
    public IReadOnlyList<int> ExceptYears { get; }

    /// <summary>
    /// Determines whether the rule applies to the supplied territory and Gregorian year.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year being resolved.</param>
    /// <returns><see langword="true" /> if the rule applies; otherwise <see langword="false" />.</returns>
    public bool AppliesTo(string territory, int year)
    {
        ThrowHelper.ThrowIfNull(territory);

        if (this.FromYear.HasValue && year < this.FromYear.Value)
            return false;

        if (this.ToYear.HasValue && year > this.ToYear.Value)
            return false;

        if (this.OnlyYears.Count > 0 && !this.OnlyYears.Contains(year))
            return false;

        if (this.ExceptYears.Contains(year))
            return false;

        return this.MatchesTerritory(territory);
    }

    /// <summary>
    /// Determines whether the rule's territories include, or are a parent of, the supplied territory.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <returns><see langword="true" /> if the territory matches; otherwise <see langword="false" />.</returns>
    public bool MatchesTerritory(string territory)
    {
        ThrowHelper.ThrowIfNull(territory);

        if (this.Territories.Count == 0)
            return true;

        foreach (string scoped in this.Territories)
        {
            if (string.Equals(scoped, territory, StringComparison.OrdinalIgnoreCase))
                return true;

            if (territory.StartsWith(scoped + "-", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the specificity of this rule's territory match against the supplied territory, used to let a narrower
    /// rule shadow a broader same-concept rule.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <returns>
    /// The length of the most specific matching territory code (an exact or parent match), <c>0</c> when the rule is
    /// global (no territories), or <c>-1</c> when the rule does not match.
    /// </returns>
    public int MatchSpecificity(string territory)
    {
        ThrowHelper.ThrowIfNull(territory);

        if (this.Territories.Count == 0)
            return 0;

        int best = -1;
        foreach (string scoped in this.Territories)
        {
            if (string.Equals(scoped, territory, StringComparison.OrdinalIgnoreCase)
                || territory.StartsWith(scoped + "-", StringComparison.OrdinalIgnoreCase))
            {
                best = Math.Max(best, scoped.Length);
            }
        }

        return best;
    }
}
