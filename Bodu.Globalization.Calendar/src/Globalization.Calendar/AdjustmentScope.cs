// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentScope.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Constrains where a reusable adjustment policy may apply: by territory, calendar, category, notable-date concept, or
/// specific rule.
/// </summary>
/// <remarks>
/// <para>
/// Each dimension is independent. A dimension with no entries imposes no restriction; a dimension with entries requires
/// the candidate value to be one of them. An empty scope therefore matches every rule, while a populated scope must
/// satisfy every populated dimension.
/// </para>
/// <para>
/// A rule reference names one concept's rule as <c>notableDateRef/ruleRef</c>, which is how the loader records a
/// document's <c>&lt;Rule notableDateRef="…" ruleRef="…" /&gt;</c> scope entry. A bare rule identifier, with no
/// concept, matches a rule with that identifier in any concept; the loader produces one only from a JSON
/// <c>ruleRefs</c> string, or when reading a version 1 binary pack, whose scopes stored the rule identifier alone.
/// </para>
/// </remarks>
/// <seealso cref="AdjustmentPolicy" /> <seealso href="../guides/calendar/adjustment-rules.html">Observance adjustment
/// rules (guide)</seealso>
public sealed class AdjustmentScope
{
    /// <summary>The separator between the concept and the rule in a qualified rule reference.</summary>
    private const string RuleRefSeparator = "/";

    /// <summary>
    /// Initializes a new instance of the <see cref="AdjustmentScope" /> class.
    /// </summary>
    /// <param name="territories">The territories the policy is limited to, if any.</param>
    /// <param name="calendars">The calendar systems the policy is limited to, if any.</param>
    /// <param name="categories">The categories the policy is limited to, if any.</param>
    /// <param name="notableDateRefs">The notable-date concepts the policy is limited to, if any.</param>
    /// <param name="ruleRefs">
    /// The rules the policy is limited to, if any, each a <c>notableDateRef/ruleRef</c> reference or a bare rule
    /// identifier.
    /// </param>
    /// <param name="fromYear">The first year the policy applies, or <see langword="null" /> for no lower bound.</param>
    /// <param name="toYear">The last year the policy applies, or <see langword="null" /> for no upper bound.</param>
    /// <param name="onlyYears">The explicit inclusion years, or <see langword="null" /> for no restriction.</param>
    /// <param name="exceptYears">The explicit exclusion years, or <see langword="null" /> for no exclusions.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="territories" />, <paramref name="calendars" />, <paramref name="categories" />,
    /// <paramref name="notableDateRefs" />, or <paramref name="ruleRefs" /> is <see langword="null" />.
    /// </exception>
    public AdjustmentScope(
        IEnumerable<string> territories,
        IEnumerable<CalendarSystem> calendars,
        IEnumerable<NotableDateCategory> categories,
        IEnumerable<string> notableDateRefs,
        IEnumerable<string> ruleRefs,
        int? fromYear = null,
        int? toYear = null,
        IEnumerable<int>? onlyYears = null,
        IEnumerable<int>? exceptYears = null)
    {
        ThrowHelper.ThrowIfNull(territories);
        ThrowHelper.ThrowIfNull(calendars);
        ThrowHelper.ThrowIfNull(categories);
        ThrowHelper.ThrowIfNull(notableDateRefs);
        ThrowHelper.ThrowIfNull(ruleRefs);

        Territories = [.. territories];
        Calendars = [.. calendars];
        Categories = [.. categories];
        NotableDateRefs = [.. notableDateRefs];
        RuleRefs = [.. ruleRefs];
        FromYear = fromYear;
        ToYear = toYear;
        OnlyYears = onlyYears?.ToArray() ?? [];
        ExceptYears = exceptYears?.ToArray() ?? [];
    }

    /// <summary>
    /// Gets a shared <see cref="AdjustmentScope" /> that imposes no restriction.
    /// </summary>
    /// <value>An empty, global scope.</value>
    public static AdjustmentScope Global { get; } = new AdjustmentScope(
        [],
        [],
        [],
        [],
        []);

    /// <summary>
    /// Gets the territories the policy is limited to.
    /// </summary>
    /// <value>The scoped territory codes; empty when unrestricted.</value>
    public IReadOnlyList<string> Territories { get; }

    /// <summary>
    /// Gets the calendar systems the policy is limited to.
    /// </summary>
    /// <value>The scoped calendar systems; empty when unrestricted.</value>
    public IReadOnlyList<CalendarSystem> Calendars { get; }

    /// <summary>
    /// Gets the categories the policy is limited to.
    /// </summary>
    /// <value>The scoped categories; empty when unrestricted.</value>
    public IReadOnlyList<NotableDateCategory> Categories { get; }

    /// <summary>
    /// Gets the notable-date concepts the policy is limited to.
    /// </summary>
    /// <value>The scoped notable-date identifiers; empty when unrestricted.</value>
    public IReadOnlyList<string> NotableDateRefs { get; }

    /// <summary>
    /// Gets the rules the policy is limited to.
    /// </summary>
    /// <value>
    /// The scoped rule references, each <c>notableDateRef/ruleRef</c> or a bare rule identifier that matches the rule
    /// in any concept; empty when unrestricted.
    /// </value>
    public IReadOnlyList<string> RuleRefs { get; }

    /// <summary>
    /// Gets the first year the policy applies.
    /// </summary>
    /// <value>The lower year bound, or <see langword="null" /> when unbounded.</value>
    public int? FromYear { get; }

    /// <summary>
    /// Gets the last year the policy applies.
    /// </summary>
    /// <value>The upper year bound, or <see langword="null" /> when unbounded.</value>
    public int? ToYear { get; }

    /// <summary>
    /// Gets the explicit inclusion years.
    /// </summary>
    /// <value>The years the policy is restricted to; empty when unrestricted.</value>
    public IReadOnlyList<int> OnlyYears { get; }

    /// <summary>
    /// Gets the explicit exclusion years.
    /// </summary>
    /// <value>The years the policy is suppressed for; empty when there are none.</value>
    public IReadOnlyList<int> ExceptYears { get; }

    /// <summary>
    /// Determines whether the policy may apply to a rule with the supplied attributes in the supplied year.
    /// </summary>
    /// <param name="territory">The territory being resolved.</param>
    /// <param name="calendar">The calendar system of the rule.</param>
    /// <param name="category">The effective category of the rule.</param>
    /// <param name="notableDateId">The identifier of the rule's notable-date concept.</param>
    /// <param name="ruleId">The identifier of the rule.</param>
    /// <param name="year">The Gregorian year of the occurrence the policy would adjust.</param>
    /// <returns>
    /// <see langword="true" /> if every populated dimension is satisfied; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The year dimensions combine like a rule's applicability: <see cref="FromYear" /> and <see cref="ToYear" /> bound
    /// an inclusive range, <see cref="OnlyYears" /> further restricts to an explicit set when non-empty, and
    /// <see cref="ExceptYears" /> removes individual years.
    /// </remarks>
    public bool Matches(
        string territory,
        CalendarSystem calendar,
        NotableDateCategory category,
        string notableDateId,
        string ruleId,
        int year)
    {
        if (FromYear.HasValue && year < FromYear.Value)
            return false;

        if (ToYear.HasValue && year > ToYear.Value)
            return false;

        if (OnlyYears.Count > 0 && !OnlyYears.Contains(year))
            return false;

        if (ExceptYears.Contains(year))
            return false;

        if (Territories.Count > 0 && !Territories.Any(t => MatchesTerritory(t, territory)))
            return false;

        if (Calendars.Count > 0 && !Calendars.Contains(calendar))
            return false;

        if (Categories.Count > 0 && !Categories.Contains(category))
            return false;

        if (NotableDateRefs.Count > 0 && !NotableDateRefs.Contains(notableDateId, StringComparer.OrdinalIgnoreCase))
            return false;

        if (RuleRefs.Count > 0 && !RuleRefs.Any(r => MatchesRuleRef(r, notableDateId, ruleId)))
            return false;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether any rule reference names its concept, which the binary pack format records with
    /// a format version that earlier readers reject rather than misread.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when at least one entry of <see cref="RuleRefs" /> is a <c>notableDateRef/ruleRef</c>
    /// reference; otherwise <see langword="false" />.
    /// </value>
    internal bool HasQualifiedRuleRefs =>
        RuleRefs.Any(r => r.Contains(RuleRefSeparator, StringComparison.Ordinal));

    /// <summary>
    /// Builds the rule reference a scope stores for a document's rule scope entry.
    /// </summary>
    /// <param name="notableDateRef">
    /// The concept that owns the rule, or <see langword="null" /> when none is named.
    /// </param>
    /// <param name="ruleRef">The rule identifier.</param>
    /// <returns><c>notableDateRef/ruleRef</c>, or <paramref name="ruleRef" /> alone when no concept is named.</returns>
    internal static string QualifyRuleRef(string? notableDateRef, string ruleRef) =>
        string.IsNullOrEmpty(notableDateRef) ? ruleRef : notableDateRef + RuleRefSeparator + ruleRef;

    /// <summary>
    /// Determines whether a scoped rule reference names the supplied rule.
    /// </summary>
    /// <param name="scoped">The rule reference declared by the scope.</param>
    /// <param name="notableDateId">The identifier of the rule's notable-date concept.</param>
    /// <param name="ruleId">The identifier of the rule.</param>
    /// <returns>
    /// <see langword="true" /> when a qualified reference names the concept and the rule, or a bare reference names the
    /// rule; otherwise <see langword="false" />. Identifiers compare case-insensitively.
    /// </returns>
    private static bool MatchesRuleRef(string scoped, string notableDateId, string ruleId)
    {
        int separator = scoped.IndexOf(RuleRefSeparator, StringComparison.Ordinal);
        if (separator < 0)
            return string.Equals(scoped, ruleId, StringComparison.OrdinalIgnoreCase);

        return scoped.AsSpan(0, separator).Equals(notableDateId, StringComparison.OrdinalIgnoreCase)
            && scoped.AsSpan(separator + 1).Equals(ruleId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether a scoped territory includes, or is a parent of, the requested territory.
    /// </summary>
    /// <param name="scoped">The territory declared by the scope.</param>
    /// <param name="territory">The requested territory.</param>
    /// <returns><see langword="true" /> if the territory matches; otherwise <see langword="false" />.</returns>
    private static bool MatchesTerritory(string scoped, string territory) =>
        string.Equals(scoped, territory, StringComparison.OrdinalIgnoreCase)
        || territory.StartsWith(scoped + "-", StringComparison.OrdinalIgnoreCase);
}
