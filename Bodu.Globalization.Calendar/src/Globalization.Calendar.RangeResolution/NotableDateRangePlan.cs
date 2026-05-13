// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePlan.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Captures the per-request resolution plan computed by <see cref="NotableDateRangePlanner" /> from a
/// <see cref="NotableDateRangeRequest" /> and a <see cref="RuleStaticAnalysis" />.
/// </summary>
/// <remarks>
/// <para>
/// The plan describes which rules are eligible to contribute to the request, which civil years they will be materialised against,
/// and which years of each algorithmic anchor must be computed. The pipeline iterates the plan exactly — no rule is processed and
/// no algorithm is invoked outside of what the plan authorises.
/// </para>
/// <para>
/// Cross-year observance roll-overs (for example, <c>31 Dec</c> rolling forward to <c>3 Jan</c>) are handled by a fringe pass that
/// the pipeline runs after the main pass. The plan exposes <see cref="FringeYears" /> so the pipeline knows which adjacent years
/// to scan for adjustment-driven candidates without polluting <see cref="CandidateYears" />.
/// </para>
/// </remarks>
internal sealed class NotableDateRangePlan
{
    /// <summary>The civil years that must be resolved for each algorithmic anchor rule name.</summary>
    private readonly Dictionary<string, IReadOnlyList<int>> _anchorYearsByName;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateRangePlan" /> class.
    /// </summary>
    /// <param name="request">The originating request.</param>
    /// <param name="eligibleRules">The rule profiles that may contribute to the request.</param>
    /// <param name="candidateYears">The civil years considered by the main pass for direct rule materialisation.</param>
    /// <param name="fringeYears">The adjacent civil years scanned by the fringe pass for adjustment-driven candidates.</param>
    /// <param name="fringeStartDate">The inclusive start of the fringe scan window.</param>
    /// <param name="fringeEndDate">The inclusive end of the fringe scan window.</param>
    /// <param name="anchorYearsByName">The civil years per algorithmic anchor that must be computed.</param>
    public NotableDateRangePlan(
        NotableDateRangeRequest request,
        IReadOnlyList<RuleStaticProfile> eligibleRules,
        IReadOnlyList<int> candidateYears,
        IReadOnlyList<int> fringeYears,
        DateTime fringeStartDate,
        DateTime fringeEndDate,
        Dictionary<string, IReadOnlyList<int>> anchorYearsByName)
    {
        Request = request;
        EligibleRules = eligibleRules;
        CandidateYears = candidateYears;
        FringeYears = fringeYears;
        FringeStartDate = fringeStartDate;
        FringeEndDate = fringeEndDate;
        _anchorYearsByName = anchorYearsByName;
    }

    /// <summary>
    /// Gets the originating request.
    /// </summary>
    /// <returns>The <see cref="NotableDateRangeRequest" /> the plan was computed for. Never <see langword="null" />.</returns>
    public NotableDateRangeRequest Request { get; }

    /// <summary>
    /// Gets the rule profiles that may contribute to the request, in input order.
    /// </summary>
    /// <returns>A read-only list of <see cref="RuleStaticProfile" /> entries, in stable input order.</returns>
    public IReadOnlyList<RuleStaticProfile> EligibleRules { get; }

    /// <summary>
    /// Gets the civil years considered by the main materialisation pass — one entry per year that the request window spans.
    /// </summary>
    /// <returns>An ordered list of four-digit civil years. Never <see langword="null" /> and never empty for a valid request.</returns>
    public IReadOnlyList<int> CandidateYears { get; }

    /// <summary>
    /// Gets the adjacent civil years scanned by the fringe pass for rules whose observance adjustment may roll a date from outside
    /// the request window into it. Empty when the request window does not touch a year boundary inside the fringe distance.
    /// </summary>
    /// <returns>An ordered list of four-digit civil years. Empty when no fringe scan is required; never <see langword="null" />.</returns>
    public IReadOnlyList<int> FringeYears { get; }

    /// <summary>
    /// Gets the inclusive start of the fringe scan window — the request start minus the fringe distance.
    /// </summary>
    /// <returns>The earliest <see cref="DateTime" /> the fringe pass considers when looking for adjustment-driven candidates.</returns>
    public DateTime FringeStartDate { get; }

    /// <summary>
    /// Gets the inclusive end of the fringe scan window — the request end plus the fringe distance.
    /// </summary>
    /// <returns>The latest <see cref="DateTime" /> the fringe pass considers when looking for adjustment-driven candidates.</returns>
    public DateTime FringeEndDate { get; }

    /// <summary>
    /// Gets the civil years that must be resolved for each algorithmic anchor name.
    /// </summary>
    /// <param name="anchorRuleName">The algorithmic anchor rule name.</param>
    /// <returns>The required civil years; an empty list when the anchor has no dependents that touch the request window.</returns>
    public IReadOnlyList<int> GetAnchorYears(string anchorRuleName) =>
        _anchorYearsByName.TryGetValue(anchorRuleName, out IReadOnlyList<int>? list)
            ? list
            : [];

    /// <summary>
    /// Enumerates the algorithmic anchor names whose computation is demanded by at least one eligible rule.
    /// </summary>
    /// <returns>The required anchor names.</returns>
    public IEnumerable<string> RequiredAnchorNames() => _anchorYearsByName.Keys;
}
