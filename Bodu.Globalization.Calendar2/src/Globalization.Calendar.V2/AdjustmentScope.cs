// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentScope.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

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
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public AdjustmentScope(
        IEnumerable<string> territories,
        IEnumerable<CalendarSystem> calendars,
        IEnumerable<NotableDateCategory> categories,
        IEnumerable<string> notableDateRefs,
        IEnumerable<string> ruleRefs)
    {
        ThrowHelper.ThrowIfNull(territories);
        ThrowHelper.ThrowIfNull(calendars);
        ThrowHelper.ThrowIfNull(categories);
        ThrowHelper.ThrowIfNull(notableDateRefs);
        ThrowHelper.ThrowIfNull(ruleRefs);

        this.Territories = territories.ToArray();
        this.Calendars = calendars.ToArray();
        this.Categories = categories.ToArray();
        this.NotableDateRefs = notableDateRefs.ToArray();
        this.RuleRefs = ruleRefs.ToArray();
    }

    /// <summary>
    /// Gets a shared <see cref="AdjustmentScope" /> that imposes no restriction.
    /// </summary>
    /// <returns>An empty, global scope.</returns>
    public static AdjustmentScope Global { get; } = new AdjustmentScope(
        Array.Empty<string>(),
        Array.Empty<CalendarSystem>(),
        Array.Empty<NotableDateCategory>(),
        Array.Empty<string>(),
        Array.Empty<string>());

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
    /// Determines whether the policy may apply to a rule with the supplied attributes.
    /// </summary>
    /// <param name="territory">The territory being resolved.</param>
    /// <param name="calendar">The calendar system of the rule.</param>
    /// <param name="category">The effective category of the rule.</param>
    /// <param name="notableDateId">The identifier of the rule's notable-date concept.</param>
    /// <param name="ruleId">The identifier of the rule.</param>
    /// <returns>
    /// <see langword="true" /> if every populated dimension is satisfied; otherwise <see langword="false" />.
    /// </returns>
    public bool Matches(
        string territory,
        CalendarSystem calendar,
        NotableDateCategory category,
        string notableDateId,
        string ruleId)
    {
        if (this.Territories.Count > 0 && !this.Territories.Any(t => MatchesTerritory(t, territory)))
            return false;

        if (this.Calendars.Count > 0 && !this.Calendars.Contains(calendar))
            return false;

        if (this.Categories.Count > 0 && !this.Categories.Contains(category))
            return false;

        if (this.NotableDateRefs.Count > 0 && !this.NotableDateRefs.Contains(notableDateId, StringComparer.OrdinalIgnoreCase))
            return false;

        if (this.RuleRefs.Count > 0 && !this.RuleRefs.Contains(ruleId, StringComparer.OrdinalIgnoreCase))
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
