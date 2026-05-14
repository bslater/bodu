// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleOccurrenceResolver.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Resolves base notable-date occurrences for a requested chronological window.
/// </summary>
/// <remarks>
/// <para>
/// This resolver is responsible only for materializing rule occurrences. It does not apply observance adjustments.
/// </para>
/// <para>
/// Rules that are calculated as offsets from a shared anchor are resolved through the calculation-anchor cache and
/// narrowed through <see cref="AnchorRelativeRuleIndex" />. This allows a query window containing <c>Start of Lent</c>
/// to calculate <c>Easter Sunday</c> internally without materializing Easter Sunday into the output unless the Easter
/// rule itself also falls inside the requested window.
/// </para>
/// </remarks>
internal sealed class NotableDateRuleOccurrenceResolver
    : INotableDateRuleOccurrenceResolver
{
    private readonly IReadOnlyList<NotableDateRule> _rules;
    private readonly NotableDateRuleResolver _ruleResolver;
    private readonly ICalculationAnchorResolver _calculationAnchors;
    private readonly AnchorRelativeRuleIndex _anchorRelativeRules;
    private readonly IReadOnlyList<string> _anchorRuleNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateRuleOccurrenceResolver" /> class.
    /// </summary>
    /// <param name="rules">The rules available for occurrence resolution.</param>
    /// <param name="ruleResolver">The resolver used for direct rule-date calculation.</param>
    /// <param name="calculationAnchors">The resolver used for cached calculation anchors.</param>
    /// <param name="anchorRelativeRules">The index of anchor-relative rules.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="rules" />, <paramref name="ruleResolver" />, <paramref name="calculationAnchors" />, or
    /// <paramref name="anchorRelativeRules" /> is <see langword="null" />.
    /// </exception>
    public NotableDateRuleOccurrenceResolver(
        IReadOnlyList<NotableDateRule> rules,
        NotableDateRuleResolver ruleResolver,
        ICalculationAnchorResolver calculationAnchors,
        AnchorRelativeRuleIndex anchorRelativeRules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(ruleResolver);
        ArgumentNullException.ThrowIfNull(calculationAnchors);
        ArgumentNullException.ThrowIfNull(anchorRelativeRules);

        this._rules = rules;
        this._ruleResolver = ruleResolver;
        this._calculationAnchors = calculationAnchors;
        this._anchorRelativeRules = anchorRelativeRules;
        this._anchorRuleNames = [.. rules
            .Where(rule => rule.Strategy == DateResolutionStrategy.OffsetFromAnchor)
            .Select(rule => rule.AnchorRuleName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <inheritdoc />
    public IReadOnlyList<ResolvedNotableDateOccurrence> ResolveOccurrences(NotableDateResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<ResolvedNotableDateOccurrence> occurrences = [];
        HashSet<OccurrenceKey> seen = [];

        ResolveDirectOccurrences(request, occurrences, seen);
        ResolveAnchorRelativeOccurrences(request, occurrences, seen);

        return [.. occurrences
            .OrderBy(occurrence => occurrence.AnchorDate)
            .ThenBy(occurrence => occurrence.Rule.Priority)
            .ThenBy(occurrence => occurrence.Rule.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(occurrence => occurrence.TerritoryCode, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Resolves rules whose dates can be calculated directly by <see cref="NotableDateRuleResolver" />.
    /// </summary>
    /// <param name="request">The resolution request.</param>
    /// <param name="occurrences">The occurrence list to populate.</param>
    /// <param name="seen">The de-duplication set.</param>
    private void ResolveDirectOccurrences(
        NotableDateResolutionRequest request,
        List<ResolvedNotableDateOccurrence> occurrences,
        HashSet<OccurrenceKey> seen)
    {
        foreach (NotableDateRule rule in _rules)
        {
            if (rule.Strategy == DateResolutionStrategy.OffsetFromAnchor)
                continue;

            if (!IsRuleEligible(rule, request))
                continue;

            foreach (var year in GetCandidateYears(request))
            {
                DateTime? anchorDate = ResolveDirectAnchorDate(rule, year);

                if (anchorDate is null)
                    continue;

                AddOccurrenceIfMatch(rule, anchorDate.Value, request, occurrences, seen);
            }
        }
    }

    /// <summary>
    /// Resolves a direct rule anchor date, using the calculation-anchor cache for algorithm-backed rules.
    /// </summary>
    /// <param name="rule">The rule to resolve.</param>
    /// <param name="year">The civil year to resolve.</param>
    /// <returns>The resolved anchor date, or <see langword="null" /> when the rule does not apply.</returns>
    private DateTime? ResolveDirectAnchorDate(NotableDateRule rule, int year)
    {
        if (rule.Strategy == DateResolutionStrategy.Algorithm &&
            !string.IsNullOrWhiteSpace(rule.Name))
        {
            return _calculationAnchors.Resolve(rule.Name, year);
        }

        return _ruleResolver.ResolveAnchorDate(rule, year);
    }

    /// <summary>
    /// Resolves rules whose dates are calculated as offsets from shared calculation anchors.
    /// </summary>
    /// <param name="request">The resolution request.</param>
    /// <param name="occurrences">The occurrence list to populate.</param>
    /// <param name="seen">The de-duplication set.</param>
    private void ResolveAnchorRelativeOccurrences(
        NotableDateResolutionRequest request,
        List<ResolvedNotableDateOccurrence> occurrences,
        HashSet<OccurrenceKey> seen)
    {
        foreach (var anchorRuleName in _anchorRuleNames)
        {
            foreach (var year in GetCandidateYears(request))
            {
                DateTime? anchorDate = _calculationAnchors.Resolve(anchorRuleName, year);

                if (anchorDate is null)
                    continue;

                var minOffsetDays = (request.StartDate - anchorDate.Value.Date).Days;
                var maxOffsetDays = (request.EndDate - anchorDate.Value.Date).Days;

                IReadOnlyList<NotableDateRule> matchingRules = _anchorRelativeRules.FindRules(
                    anchorRuleName,
                    minOffsetDays,
                    maxOffsetDays);

                foreach (NotableDateRule rule in matchingRules)
                {
                    if (!IsRuleEligible(rule, request))
                        continue;

                    if (rule.OffsetDays is not { } offsetDays)
                        continue;

                    DateTime occurrenceDate;

                    try
                    {
                        occurrenceDate = anchorDate.Value.Date.AddDays(offsetDays);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        continue;
                    }

                    AddOccurrenceIfMatch(rule, occurrenceDate, request, occurrences, seen);
                }
            }
        }
    }

    /// <summary>
    /// Adds a materialized occurrence when it intersects the requested output window and satisfies the date filter.
    /// </summary>
    /// <param name="rule">The rule that produced the occurrence.</param>
    /// <param name="anchorDate">The resolved occurrence date.</param>
    /// <param name="request">The resolution request.</param>
    /// <param name="occurrences">The occurrence list to populate.</param>
    /// <param name="seen">The de-duplication set.</param>
    private void AddOccurrenceIfMatch(
        NotableDateRule rule,
        DateTime anchorDate,
        NotableDateResolutionRequest request,
        List<ResolvedNotableDateOccurrence> occurrences,
        HashSet<OccurrenceKey> seen)
    {
        foreach (var territoryCode in EnumerateApplicableTerritoryCodes(rule, request.TerritoryCode))
        {
            NotableDate notable = BuildNotableDate(rule, anchorDate.Date, territoryCode);

            if (!Intersects(request.StartDate, request.EndDate, notable.Date, notable.EndDate))
                continue;

            if (request.Filter is not null && !request.Filter.IsMatch(notable))
                continue;

            OccurrenceKey key = new(
                notable.Name,
                notable.Date,
                notable.TerritoryCode,
                notable.CalendarType);

            if (!seen.Add(key))
                continue;

            occurrences.Add(new ResolvedNotableDateOccurrence(
                rule,
                notable.Date,
                territoryCode,
                notable));
        }
    }

    /// <summary>
    /// Creates a materialized notable date from a rule and resolved anchor date.
    /// </summary>
    /// <param name="rule">The source rule.</param>
    /// <param name="date">The resolved date.</param>
    /// <param name="territoryCode">The expanded territory code.</param>
    /// <returns>The materialized notable date.</returns>
    private static NotableDate BuildNotableDate(
        NotableDateRule rule,
        DateTime date,
        string? territoryCode) =>
        new()
        {
            Date = date.Date,
            Name = rule.Name,
            Category = rule.Category,
            DurationDays = Math.Max(1, rule.DurationDays),
            IsNonWorkingDay = rule.IsNonWorkingDay == true,
            CalendarType = rule.CalendarType,
            TerritoryCode = territoryCode,
            Tags = rule.Tags,
            Comment = rule.Comment,
        };

    /// <summary>
    /// Determines whether a rule is eligible for the request before date resolution.
    /// </summary>
    /// <param name="rule">The rule to test.</param>
    /// <param name="request">The resolution request.</param>
    /// <returns><see langword="true" /> when the rule may produce a matching occurrence.</returns>
    private static bool IsRuleEligible(NotableDateRule rule, NotableDateResolutionRequest request)
    {
        if (request.Filter is not null && !request.Filter.IsRuleEligible(rule))
            return false;

        if (!RuleMayApplyToTerritory(rule, request.TerritoryCode))
            return false;

        if (request.CalendarType is not null &&
            rule.CalendarType is not null &&
            rule.CalendarType != request.CalendarType)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets conservative candidate years for resolving rules against the requested window.
    /// </summary>
    /// <param name="request">The resolution request.</param>
    /// <returns>The candidate civil years.</returns>
    private static IEnumerable<int> GetCandidateYears(NotableDateResolutionRequest request)
    {
        var startYear = request.StartDate.Year;
        var endYear = request.EndDate.Year;

        for (var year = startYear; year <= endYear; year++)
            yield return year;
    }

    /// <summary>
    /// Enumerates territory codes from a rule that are applicable to the requested context.
    /// </summary>
    /// <param name="rule">The rule to inspect.</param>
    /// <param name="requestedTerritoryCode">The requested territory code.</param>
    /// <returns>The applicable expanded territory codes.</returns>
    private static IEnumerable<string?> EnumerateApplicableTerritoryCodes(
        NotableDateRule rule,
        string? requestedTerritoryCode)
    {
        if (string.IsNullOrWhiteSpace(rule.TerritoryCode))
        {
            yield return null;
            yield break;
        }

        foreach (var territoryCode in rule.TerritoryCode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(territoryCode))
                continue;

            if (!TerritoryMatches(territoryCode, requestedTerritoryCode))
                continue;

            yield return territoryCode;
        }
    }

    /// <summary>
    /// Determines whether a rule may apply to the requested territory.
    /// </summary>
    /// <param name="rule">The rule to test.</param>
    /// <param name="requestedTerritoryCode">The requested territory code.</param>
    /// <returns><see langword="true" /> when the rule may apply.</returns>
    private static bool RuleMayApplyToTerritory(NotableDateRule rule, string? requestedTerritoryCode)
    {
        if (string.IsNullOrWhiteSpace(rule.TerritoryCode))
            return true;

        foreach (var territoryCode in rule.TerritoryCode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (TerritoryMatches(territoryCode, requestedTerritoryCode))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a rule territory and requested territory are compatible.
    /// </summary>
    /// <param name="ruleTerritoryCode">The rule territory code.</param>
    /// <param name="requestedTerritoryCode">The requested territory code.</param>
    /// <returns><see langword="true" /> when the territories are compatible.</returns>
    private static bool TerritoryMatches(string? ruleTerritoryCode, string? requestedTerritoryCode)
    {
        if (string.IsNullOrWhiteSpace(requestedTerritoryCode) ||
            string.IsNullOrWhiteSpace(ruleTerritoryCode))
        {
            return true;
        }

        if (!TerritoryCode.TryParse(requestedTerritoryCode, out TerritoryCode requested))
            return false;

        if (!TerritoryCode.TryParse(ruleTerritoryCode, out TerritoryCode owned))
            return false;

        return requested.Contains(owned) || owned.Contains(requested);
    }

    /// <summary>
    /// Determines whether two inclusive date spans intersect.
    /// </summary>
    /// <param name="leftStart">The first span start.</param>
    /// <param name="leftEnd">The first span end.</param>
    /// <param name="rightStart">The second span start.</param>
    /// <param name="rightEnd">The second span end.</param>
    /// <returns><see langword="true" /> when the spans intersect.</returns>
    private static bool Intersects(
        DateTime leftStart,
        DateTime leftEnd,
        DateTime rightStart,
        DateTime rightEnd) =>
        rightStart.Date <= leftEnd.Date &&
        rightEnd.Date >= leftStart.Date;

    /// <summary>
    /// Identifies a materialized occurrence for de-duplication.
    /// </summary>
    /// <param name="Name">The occurrence name.</param>
    /// <param name="Date">The occurrence date.</param>
    /// <param name="TerritoryCode">The occurrence territory code.</param>
    /// <param name="CalendarType">The occurrence calendar type.</param>
    private readonly record struct OccurrenceKey(
        string Name,
        DateTime Date,
        string? TerritoryCode,
        Type? CalendarType);
}
