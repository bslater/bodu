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
/// </remarks>
/// <seealso cref="AdjustmentPolicy" /> <seealso href="../guides/calendar/adjustment-rules.html">Observance adjustment
/// rules (guide)</seealso>
public sealed class AdjustmentScope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdjustmentScope" /> class.
    /// </summary>
    /// <param name="territories">The territories the policy is limited to, if any.</param>
    /// <param name="calendars">The calendar systems the policy is limited to, if any.</param>
    /// <param name="categories">The categories the policy is limited to, if any.</param>
    /// <param name="notableDateRefs">The notable-date concepts the policy is limited to, if any.</param>
    /// <param name="ruleRefs">The rules the policy is limited to, if any.</param>
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

        Territories = territories.ToArray();
        Calendars = calendars.ToArray();
        Categories = categories.ToArray();
        NotableDateRefs = notableDateRefs.ToArray();
        RuleRefs = ruleRefs.ToArray();
        FromYear = fromYear;
        ToYear = toYear;
        OnlyYears = onlyYears?.ToArray() ?? [];
        ExceptYears = exceptYears?.ToArray() ?? [];
    }

    /// <summary>
    /// Gets a shared <see cref="AdjustmentScope" /> that imposes no restriction.
    /// </summary>
    /// <returns>An empty, global scope.</returns>
    public static AdjustmentScope Global { get; } = new AdjustmentScope(
        [],
        [],
        [],
        [],
        []);

    /// <summary>
    /// Gets the territories the policy is limited to.
    /// </summary>
    /// <returns>The scoped territory codes; empty when unrestricted.</returns>
    public IReadOnlyList<string> Territories { get; }

    /// <summary>
    /// Gets the calendar systems the policy is limited to.
    /// </summary>
    /// <returns>The scoped calendar systems; empty when unrestricted.</returns>
    public IReadOnlyList<CalendarSystem> Calendars { get; }

    /// <summary>
    /// Gets the categories the policy is limited to.
    /// </summary>
    /// <returns>The scoped categories; empty when unrestricted.</returns>
    public IReadOnlyList<NotableDateCategory> Categories { get; }

    /// <summary>
    /// Gets the notable-date concepts the policy is limited to.
    /// </summary>
    /// <returns>The scoped notable-date identifiers; empty when unrestricted.</returns>
    public IReadOnlyList<string> NotableDateRefs { get; }

    /// <summary>
    /// Gets the rules the policy is limited to.
    /// </summary>
    /// <returns>The scoped rule identifiers; empty when unrestricted.</returns>
    public IReadOnlyList<string> RuleRefs { get; }

    /// <summary>
    /// Gets the first year the policy applies.
    /// </summary>
    /// <returns>The lower year bound, or <see langword="null" /> when unbounded.</returns>
    public int? FromYear { get; }

    /// <summary>
    /// Gets the last year the policy applies.
    /// </summary>
    /// <returns>The upper year bound, or <see langword="null" /> when unbounded.</returns>
    public int? ToYear { get; }

    /// <summary>
    /// Gets the explicit inclusion years.
    /// </summary>
    /// <returns>The years the policy is restricted to; empty when unrestricted.</returns>
    public IReadOnlyList<int> OnlyYears { get; }

    /// <summary>
    /// Gets the explicit exclusion years.
    /// </summary>
    /// <returns>The years the policy is suppressed for; empty when there are none.</returns>
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

        if (RuleRefs.Count > 0 && !RuleRefs.Contains(ruleId, StringComparer.OrdinalIgnoreCase))
            return false;

        return true;
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
